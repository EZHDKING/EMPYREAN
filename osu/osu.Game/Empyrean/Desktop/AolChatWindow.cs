// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Empyrean.UI;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.Chat;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Empyrean.Desktop
{
    /// <summary>
    /// An AOL-Instant-Messenger-styled chat window. It shows osu! chat in plain IRC text mode
    /// (timestamp + nick + message) on a white panel with the classic AOL yellow/blue trim, and
    /// posts what you type to the current channel. If chat isn't connected it explains so.
    /// </summary>
    public partial class AolChatWindow : Win95Window
    {
        [Resolved(canBeNull: true)]
        private ChannelManager channels { get; set; }

        private FillFlowContainer messageFlow;
        private BasicTextBox input;
        private Channel boundChannel;

        private static readonly Color4 aol_blue = new Color4(0, 51, 153, 255);
        private static readonly Color4 aol_yellow = new Color4(255, 204, 0, 255);

        public AolChatWindow()
            : base("AOL Instant Messenger", FontAwesome.Solid.Comments)
        {
            Name = "AOL Messenger";
            Size = new Vector2(460, 380);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.White },

                    // AOL-style banner.
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 34,
                        Children = new Drawable[]
                        {
                            new Box { RelativeSizeAxes = Axes.Both, Colour = aol_blue },
                            new Box { RelativeSizeAxes = Axes.X, Height = 3, Anchor = Anchor.BottomLeft, Origin = Anchor.BottomLeft, Colour = aol_yellow },
                            new SpriteIcon { Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Margin = new MarginPadding { Left = 8 }, Size = new Vector2(20), Icon = FontAwesome.Solid.Running, Colour = aol_yellow },
                            new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Margin = new MarginPadding { Left = 36 },
                                Text = "AOL Instant Messenger — osu! chat",
                                Font = OsuFont.GetFont(size: 16, weight: FontWeight.Bold),
                                Colour = Color4.White,
                            },
                        },
                    },

                    // Message area.
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Top = 38, Bottom = 36, Left = 4, Right = 4 },
                        Children = new Drawable[]
                        {
                            new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.White },
                            new Win95Bevel(Win95Bevel.Style.Field),
                            new BasicScrollContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Padding = new MarginPadding(4),
                                ScrollbarVisible = true,
                                Child = messageFlow = new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FillDirection.Vertical,
                                    Spacing = new Vector2(0, 1),
                                },
                            },
                        },
                    },

                    // Input row.
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 32,
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        Children = new Drawable[]
                        {
                            new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.FACE },
                            input = new BasicTextBox
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 26,
                                Width = 0.8f,
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Margin = new MarginPadding { Left = 4 },
                                PlaceholderText = "Type a message and press Enter…",
                                CommitOnFocusLost = false,
                            },
                            Win95Button.Text("Send", send, 70, 24).With(b =>
                            {
                                b.Anchor = Anchor.CentreRight;
                                b.Origin = Anchor.CentreRight;
                                b.Margin = new MarginPadding { Right = 4 };
                            }),
                        },
                    },
                },
            });

            if (input != null)
                input.OnCommit += (_, __) => send();

            bindChannel();
        }

        private void bindChannel()
        {
            if (channels == null)
            {
                addLine("*** osu! chat is unavailable. Sign in to use chat.", Win95.TEXT_DISABLED);
                return;
            }

            boundChannel = channels.CurrentChannel?.Value ?? channels.JoinedChannels.FirstOrDefault();

            if (boundChannel == null)
            {
                addLine("*** No channel joined yet. Channels appear here once chat connects.", Win95.TEXT_DISABLED);
                return;
            }

            addLine($"*** Now chatting in {boundChannel.Name}", aol_blue);

            // Show recent backlog.
            foreach (var m in boundChannel.Messages.TakeLast(50))
                addMessage(m);

            boundChannel.NewMessagesArrived += onNewMessages;
        }

        private void onNewMessages(System.Collections.Generic.IEnumerable<Message> messages)
        {
            // Marshal onto the update thread for UI safety.
            Schedule(() =>
            {
                foreach (var m in messages)
                    addMessage(m);
            });
        }

        private void addMessage(Message m)
        {
            string time = m.Timestamp.LocalDateTime.ToString("HH:mm");
            string nick = m.Sender?.Username ?? "?";
            addLine($"[{time}] <{nick}> {m.Content}", Win95.TEXT);
        }

        private void addLine(string text, Color4 colour)
        {
            messageFlow?.Add(new OsuSpriteText
            {
                Text = text,
                Font = OsuFont.GetFont(size: 13),
                Colour = colour,
                RelativeSizeAxes = Axes.X,
                AllowMultiline = true,
            });
        }

        private void send()
        {
            string text = input?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(text) || channels == null || boundChannel == null)
                return;

            try
            {
                channels.PostMessage(text, target: boundChannel);
            }
            catch (Exception ex)
            {
                addLine($"*** could not send: {ex.Message}", Win95.TEXT_DISABLED);
            }

            if (input != null)
                input.Text = string.Empty;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            if (boundChannel != null)
                boundChannel.NewMessagesArrived -= onNewMessages;
        }
    }
}
