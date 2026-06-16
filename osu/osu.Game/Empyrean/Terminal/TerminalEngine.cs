// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Game.Empyrean.Terminal
{
    /// <summary>
    /// A single terminal command. Pure command pattern: no drawables, no per-frame work.
    /// Execution returns text output (already formatted) that the console prints.
    /// </summary>
    public class TerminalCommand
    {
        public string Name { get; }

        /// <summary>Optional short aliases (e.g. "q" for "quit").</summary>
        public IReadOnlyList<string> Aliases { get; }

        public string Usage { get; }

        public string Description { get; }

        /// <summary>
        /// Executes the command. <paramref name="args"/> excludes the command token itself.
        /// Returns lines to print. Throwing is allowed; the console will surface a clean error.
        /// </summary>
        public Func<string[], IEnumerable<string>> Execute { get; }

        public TerminalCommand(string name, string usage, string description,
                               Func<string[], IEnumerable<string>> execute,
                               IReadOnlyList<string>? aliases = null)
        {
            Name = name;
            Usage = usage;
            Description = description;
            Execute = execute;
            Aliases = aliases ?? Array.Empty<string>();
        }
    }

    /// <summary>
    /// The EMPYREAN command engine.
    ///
    /// Deliberately UI-free so it can be unit tested in isolation (PROJECT.md §21) and so the
    /// expensive parts of the game (config, API, ruleset) are reached only through small
    /// callback hooks injected at construction time. The engine itself allocates nothing per
    /// frame — it only runs when the user submits a line.
    /// </summary>
    public class TerminalEngine
    {
        private readonly Dictionary<string, TerminalCommand> commands = new Dictionary<string, TerminalCommand>(StringComparer.OrdinalIgnoreCase);
        private readonly List<TerminalCommand> ordered = new List<TerminalCommand>();

        private readonly List<string> history = new List<string>();
        public IReadOnlyList<string> History => history;

        public void Register(TerminalCommand command)
        {
            ordered.Add(command);
            commands[command.Name] = command;
            foreach (string alias in command.Aliases)
                commands[alias] = command;
        }

        public IReadOnlyList<TerminalCommand> Commands => ordered;

        /// <summary>
        /// Returns command names that start with the given prefix, for autocomplete (§6.2).
        /// Case-insensitive, allocation only on demand.
        /// </summary>
        public IReadOnlyList<string> Complete(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return ordered.Select(c => c.Name).ToList();

            return ordered.Select(c => c.Name)
                          .Where(n => n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                          .ToList();
        }

        /// <summary>
        /// Parses and runs a raw input line. Always returns printable output; never throws.
        /// </summary>
        public IReadOnlyList<string> Run(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return Array.Empty<string>();

            history.Add(line);

            string[] tokens = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string name = tokens[0];
            string[] args = tokens.Skip(1).ToArray();

            if (!commands.TryGetValue(name, out var command))
                return new[] { $"unknown command: {name}  (type 'help')" };

            try
            {
                return command.Execute(args).ToList();
            }
            catch (Exception ex)
            {
                // §15: clear, actionable, non-blocking. No stack-trace rendering.
                return new[] { $"error: {ex.Message}" };
            }
        }
    }
}
