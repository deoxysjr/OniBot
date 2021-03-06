﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OniBot.Infrastructure;
using OniBot.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OniBot.Commands
{
    [Group("debug")]
    [ConfigurationPrecondition]
    public class DebugCommands : ModuleBase<SocketCommandContext>, IBotCommand
    {
        [Command("config")]
        [Summary("Sends the currently running config")]
        public async Task DumpConfig(
        [Summary("[Optional] If supplied, uploads just the single config to Discord")]string config = null)
        {
            var files = Directory.GetFiles("./config/", "*.json", SearchOption.AllDirectories).ToList();

            if (string.IsNullOrWhiteSpace(config))
            {
                foreach (var file in files)
                {
                    var filename = file.Substring(0);
                    var contents = File.ReadAllBytes(file);
                    await Context.User.SendFileAsync(contents, filename).ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                }
            }
            else
            {
                files = files.Where(a => a.Contains(config)).ToList();
                if (files.Count == 0)
                {
                    await Context.User.SendMessageAsync("No files found").ConfigureAwait(false);
                }

                foreach (var file in files)
                {
                    var filename = file.Substring(0);
                    var contents = File.ReadAllBytes(file);
                    await Context.User.SendFileAsync(contents, filename).ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                }
            }
        }

        [Command("channel")]
        [Summary("Dumps the current channel")]
        public async Task DumpChannel()
        {
            var props = DumpProps(Context.Channel);
            await Context.User.SendFileAsync(Encoding.UTF8.GetBytes(props), $"{Context.Channel.Name}.txt").ConfigureAwait(false);
        }

        [Command("guild")]
        [Summary("Dumps the current guild")]
        public async Task DumpGuild()
        {
            var props = DumpProps(Context.Guild);
            await Context.User.SendFileAsync(Encoding.UTF8.GetBytes(props), $"{Context.Guild.Name}.txt").ConfigureAwait(false);
        }

        [Command("bot")]
        [Summary("Gets the current run state of the bot")]
        public async Task DumpMyself()
        {
            var props = DumpProps(Context.Client.CurrentUser);
            await Context.User.SendMessageAsync($"```{props}```").ConfigureAwait(false);

            var cPerms = GetChannelPerms(Context.Client.CurrentUser as IGuildUser);
            await Context.User.SendMessageAsync($"```{"Channel Permissions".PadRight(20)}{string.Join(", ", cPerms)}```").ConfigureAwait(false);

            var gPerms = GetGuildPerms(Context.Client.CurrentUser as IGuildUser);
            await Context.User.SendMessageAsync($"```{"Guild Permissions".PadRight(20)}{string.Join(", ", gPerms)}```").ConfigureAwait(false);
        }

        [Command("user")]
        [Summary("Gets the current run state of a user")]
        public async Task DumpUser(SocketGuildUser user)
        {
            var props = DumpProps(user);
            await Context.User.SendMessageAsync($"```{props}```").ConfigureAwait(false);

            var cPerms = GetChannelPerms(user);
            await Context.User.SendMessageAsync($"```{"Channel Permissions".PadRight(20)}{string.Join(", ", cPerms)}```").ConfigureAwait(false);

            var gPerms = GetGuildPerms(user);
            await Context.User.SendMessageAsync($"```{"Guild Permissions".PadRight(20)}{string.Join(", ", gPerms)}```").ConfigureAwait(false);
        }

        [Command("chat")]
        [Summary("Gets the current run state of a user")]
        public async Task DumpChat(int count)
        {
            var messages = await Context.Channel.GetMessagesAsync(limit: count, fromMessageId: Context.Message.Id, dir: Direction.Before).ToList().ConfigureAwait(false);

            foreach (var messageContainer in messages)
            {
                foreach (var message in messageContainer)
                {
                    var props = DumpProps(message);

                    await Context.User.SendMessageAsync($"```{props}```").ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                }
            }
        }

        [Command("info")]
        [Summary("Provides basic information about the bot and its environment")]
        public async Task Info()
        {
            var application = await Context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
            var infoString =
                $"{Format.Bold("Info")}\n" +
                $"- Author: {application.Owner.Username} (ID {application.Owner.Id})\n" +
                $"- Library: Discord.Net ({DiscordConfig.Version})\n" +
                $"- Runtime: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture}\n" +
                $"- Uptime: {GetUptime()}\n\n" +

                $"{Format.Bold("Stats")}\n" +
                $"- Heap Size: {GetHeapSize()} MB";
            if (Context.Client is DiscordSocketClient guildClient)
            {
                infoString += "\n" +
                $"- Guilds: {guildClient.Guilds.Count}\n" +
                $"- Channels: {guildClient.Guilds.Sum(g => g.Channels.Count)}\n" +
                $"- Users: {guildClient.Guilds.Sum(g => g.MemberCount)}";
            }

            await Context.User.SendMessageAsync(infoString).ConfigureAwait(false);
        }

        private static string GetUptime()
            => (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss");

        private static string GetHeapSize() => Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString();

        private string DumpProps(object objects)
        {
            var sb = new StringBuilder();
            var userType = objects.GetType();
            sb.AppendLine($"{"Type".PadRight(20)}{userType}");
            var props = userType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var prop in props)
            {
                var key = prop.Name.PadRight(20);
                string value = string.Empty;
                try
                {
                    var property = prop.GetValue(objects);
                    if(property == null) {
                        value = string.Empty;
                    }
                    else if(prop.PropertyType == typeof(string)) {
                        value = property.ToString();
                    }
                    else if (typeof(IEnumerable).IsAssignableFrom(property?.GetType()))
                    {
                        foreach (var item in property as IEnumerable)
                        {
                            value += item.ToString() + ", ";
                        }

                        value = value.TrimEnd(", ".ToCharArray());
                    }
                    else
                    {
                        value = property.ToString();
                    }
                }
                catch (Exception ex)
                {
                    value = $"Exception while processing property value: {ex.Message}";
                }
                sb.AppendLine($"{key}{value}");
            }
            
            return sb.ToString();
        }

        private List<ChannelPermission> GetChannelPerms(IGuildUser user)
        {
            var hasChannelPerms = new List<ChannelPermission>();
            if (Context.Channel is IGuildChannel channel)
            {
                var userChannelPerms = user.GetPermissions(channel);

                var channelPerms = Enum.GetValues(typeof(ChannelPermission)).Cast<ChannelPermission>();
                
                foreach (var perm in channelPerms)
                {
                    if (userChannelPerms.Has(perm))
                    {
                        hasChannelPerms.Add(perm);
                    }
                }
            }

            return hasChannelPerms;
        }

        private List<GuildPermission> GetGuildPerms(IGuildUser user)
        {
            var userGuildPerms = user.GuildPermissions;
            var guildPerms = Enum.GetValues(typeof(GuildPermission)).Cast<GuildPermission>();

            var hasGuildPerms = new List<GuildPermission>();

            foreach (var perm in guildPerms)
            {
                if (userGuildPerms.Has(perm))
                {
                    hasGuildPerms.Add(perm);
                }
            }
            return hasGuildPerms;
        }
    }
}
