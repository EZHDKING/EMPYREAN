#!/usr/bin/env bash
# EMPYREAN — create (if missing) and push the three GitHub repositories.
# Creator: EZHD KING
#
#   EMPYREAN             <- the full project (this whole tree, buildable on clone)
#   EMPYREAN-FRAMEWORK   <- the osu-framework/ engine fork, as a standalone repo
#   EMPYREAN-RESOURCES   <- the osu-resources/ assets+shaders fork, as a standalone repo
#
# If a repo does not exist on your GitHub account it is created; if it already exists, new changes
# are committed and pushed.
#
# HOW IT'S STRUCTURED
#   EMPYREAN is a monorepo: it contains osu/, osu-framework/ and osu-resources/ so a fresh clone
#   builds with no extra steps (the project references are relative). EMPYREAN-FRAMEWORK and
#   EMPYREAN-RESOURCES are standalone exports of the two subfolders for people who want just those.
#   To avoid nesting a .git inside those subfolders (which would turn them into broken submodule
#   pointers in the monorepo), the two standalone repos are managed with SEPARATE git directories
#   (.git-empyrean-framework / .git-empyrean-resources at the root, which the monorepo ignores).
#
# REQUIREMENTS
#   - git
#   - GitHub CLI 'gh', authenticated:  gh auth login
#   (gh handles repo creation and provides the remote URL/credentials.)
#
# USAGE
#   ./push_all.sh "commit message"          # push all three under your gh account
#   GH_OWNER=myorg ./push_all.sh "message"  # push under a specific org/user
#   VISIBILITY=public ./push_all.sh "msg"   # public (default) | private

set -euo pipefail

MSG="${1:-EMPYREAN update}"
VISIBILITY="${VISIBILITY:-public}"
HERE="$(cd "$(dirname "$0")" && pwd)"

# ---- preflight -------------------------------------------------------------
command -v git >/dev/null 2>&1 || { echo "ERROR: git not found." >&2; exit 1; }
command -v gh  >/dev/null 2>&1 || { echo "ERROR: GitHub CLI 'gh' not found. Install it and run 'gh auth login'." >&2; exit 1; }
gh auth status >/dev/null 2>&1 || { echo "ERROR: gh is not authenticated. Run 'gh auth login' first." >&2; exit 1; }

# Resolve the owner (org/user) to create repos under.
OWNER="${GH_OWNER:-$(gh api user --jq .login)}"
echo "GitHub owner: ${OWNER}   visibility: ${VISIBILITY}"

# ---- helpers ---------------------------------------------------------------

# Ensure a repo exists on GitHub; create it if not. Echoes the clone URL (https) on stdout;
# all progress goes to stderr so it stays visible when the URL is captured.
ensure_repo() {
    local name="$1"
    if gh repo view "${OWNER}/${name}" >/dev/null 2>&1; then
        echo "  repo ${OWNER}/${name} exists" >&2
    else
        echo "  creating ${OWNER}/${name} (${VISIBILITY})" >&2
        gh repo create "${OWNER}/${name}" "--${VISIBILITY}" --disable-wiki >/dev/null
    fi
    echo "https://github.com/${OWNER}/${name}.git"
}

# Commit + push a work tree using an explicit git dir (so no .git is nested in the work tree).
# Args: <git-dir> <work-tree> <remote-url> <branch>
push_tree() {
    local gitdir="$1" worktree="$2" url="$3" branch="${4:-main}"

    export GIT_DIR="${gitdir}"
    export GIT_WORK_TREE="${worktree}"

    if [[ ! -d "${gitdir}" ]]; then
        git init -q
        git symbolic-ref HEAD "refs/heads/${branch}"
    fi

    # Identity (only sets locally if unset globally).
    git config user.name  >/dev/null 2>&1 || git config user.name  "EMPYREAN"
    git config user.email >/dev/null 2>&1 || git config user.email "empyrean@localhost"

    # Remote.
    if git remote get-url origin >/dev/null 2>&1; then
        git remote set-url origin "${url}"
    else
        git remote add origin "${url}"
    fi

    git add -A
    if git diff --cached --quiet 2>/dev/null && git rev-parse HEAD >/dev/null 2>&1; then
        echo "    no changes to commit"
    else
        git commit -q -m "${MSG}" || echo "    nothing to commit"
    fi

    # Push, setting upstream. Force-with-lease keeps it safe on re-pushes of an existing history.
    git branch -M "${branch}" 2>/dev/null || true
    git push -u origin "${branch}" --force-with-lease

    unset GIT_DIR GIT_WORK_TREE
}

# ---- .gitignore for the monorepo ------------------------------------------
# Keep build output and the side git dirs out of the EMPYREAN monorepo.
ensure_gitignore() {
    local gi="${HERE}/.gitignore"
    touch "${gi}"
    for pat in "/dist/" "/.git-empyrean-framework/" "/.git-empyrean-resources/" "bin/" "obj/"; do
        grep -qxF "${pat}" "${gi}" 2>/dev/null || echo "${pat}" >> "${gi}"
    done
}

# ===========================================================================
echo
echo "=== EMPYREAN-FRAMEWORK ==="
FW_URL="$(ensure_repo "EMPYREAN-FRAMEWORK")"
push_tree "${HERE}/.git-empyrean-framework" "${HERE}/osu-framework" "${FW_URL}"

echo
echo "=== EMPYREAN-RESOURCES ==="
RES_URL="$(ensure_repo "EMPYREAN-RESOURCES")"
push_tree "${HERE}/.git-empyrean-resources" "${HERE}/osu-resources" "${RES_URL}"

echo
echo "=== EMPYREAN (monorepo) ==="
ensure_gitignore
EMP_URL="$(ensure_repo "EMPYREAN")"
# The monorepo uses the normal .git at the root and includes everything (incl. osu-framework/ and
# osu-resources/ as plain files, since they carry no nested .git thanks to the side git dirs above).
push_tree "${HERE}/.git" "${HERE}" "${EMP_URL}"

echo
echo "=================================================================="
echo " Pushed:"
echo "   https://github.com/${OWNER}/EMPYREAN"
echo "   https://github.com/${OWNER}/EMPYREAN-FRAMEWORK"
echo "   https://github.com/${OWNER}/EMPYREAN-RESOURCES"
echo "=================================================================="
