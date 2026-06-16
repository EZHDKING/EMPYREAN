// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Empyrean.UI;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Empyrean.Desktop
{
    /// <summary>
    /// A Windows 95 desktop icon: a glyph above a wrapped label, selectable, launched by
    /// double-click, freely DRAGGABLE around the desktop, and right-clickable for a context menu.
    /// Positioning is managed by the desktop's icon layer; this just reports drags and clicks.
    /// </summary>
    public partial class DesktopIcon : Container
    {
        public Action OnOpen;
        // Right-click: desktop shows a context menu at the given screen-space position.
        public Action<Vector2> OnContextMenu;
        // Called while/after dragging so the desktop can persist the new position.
        public Action OnMoved;
        // Called when a drag ends, with the icon's screen-space centre, so the desktop can
        // hit-test for a drop onto a folder (collection). Return handled if it was consumed.
        public Action<DesktopIcon, Vector2> OnDropped;
        // Plain left-click (no drag): used by the desktop to manage selection.
        public Action<DesktopIcon, bool> OnClicked; // bool = ctrl/shift held (additive)

        /// <summary>The shortcut this icon represents (null for built-in program icons).</summary>
        public BeatmapShortcut Shortcut;

        /// <summary>True if this icon is a folder/collection (a valid drop target).</summary>
        public bool IsFolder;

        private readonly Box selection;
        private readonly OsuSpriteText label;
        private bool selected;

        public DesktopIcon(IconUsage icon, string text, Action onOpen, float iconSize = 32, string iconName = null)
        {
            OnOpen = onOpen;

            // Cell size scales with the icon size; label wraps under it.
            float cellW = iconSize + 44;
            float cellH = iconSize + 38;
            Size = new Vector2(cellW, cellH);

            // Prefer a bundled Win95 bitmap icon; fall back to a vector glyph if not present.
            Drawable iconDrawable = (iconName != null ? EmpyreanAssets.IconSprite(iconName) : null);
            iconDrawable ??= new SpriteIcon
            {
                Size = new Vector2(iconSize),
                Icon = icon,
                Colour = Color4.White,
            };
            iconDrawable.Anchor = Anchor.TopCentre;
            iconDrawable.Origin = Anchor.TopCentre;
            if (iconDrawable is Sprite sp)
                sp.Size = new Vector2(iconSize);

            Children = new Drawable[]
            {
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 3),
                    Padding = new MarginPadding(3),
                    Children = new Drawable[]
                    {
                        iconDrawable,
                        new Container
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            RelativeSizeAxes = Axes.X,
                            Height = iconSize - 4,
                            Children = new Drawable[]
                            {
                                selection = new Box
                                {
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                    RelativeSizeAxes = Axes.X,
                                    Height = 16,
                                    Colour = Win95.TITLE,
                                    Alpha = 0,
                                },
                                label = new OsuSpriteText
                                {
                                    Text = text,
                                    Font = OsuFont.GetFont(size: 12),
                                    Colour = Color4.White,
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                    Margin = new MarginPadding { Horizontal = 2 },
                                    AllowMultiline = true,
                                    MaxWidth = cellW - 6,
                                },
                            },
                        },
                    },
                },
            };
        }

        public string LabelText
        {
            get => label.Text.ToString();
            set => label.Text = value;
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => Alpha > 0.01f && base.ReceivePositionalInputAt(screenSpacePos);

        protected override bool OnClick(ClickEvent e)
        {
            bool additive = e.ControlPressed || e.ShiftPressed;
            // Let the desktop coordinate selection across icons (clear others unless additive).
            OnClicked?.Invoke(this, additive);
            return true;
        }

        public void setSelected(bool value)
        {
            selected = value;
            selection.Alpha = value ? 1 : 0;
        }

        public bool IsSelected => selected;

        protected override bool OnDoubleClick(DoubleClickEvent e)
        {
            OnOpen?.Invoke();
            return true;
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (e.Button == osuTK.Input.MouseButton.Right)
            {
                OnContextMenu?.Invoke(e.ScreenSpaceMousePosition);
                return true;
            }

            // Left-press selects and prepares for a potential drag.
            setSelected(true);
            return true;
        }

        // ---- dragging ----------------------------------------------------------------
        protected override bool OnDragStart(DragStartEvent e)
        {
            return true;
        }

        protected override void OnDrag(DragEvent e)
        {
            Position += e.Delta;
        }

        protected override void OnDragEnd(DragEndEvent e)
        {
            // Report the drop using the icon's screen-space centre so the desktop can test whether
            // we were released over a folder (collection). If not handled, persist the move.
            var centre = ToScreenSpace(new Vector2(DrawWidth / 2, DrawHeight / 2));
            OnDropped?.Invoke(this, centre);
            OnMoved?.Invoke();
        }
    }
}
