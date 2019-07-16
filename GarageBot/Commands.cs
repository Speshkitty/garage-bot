using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System;

namespace GarageBot
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        [Command("test")]
        public async Task NotInServer()
        {
            await ReplyAsync("tested");
        }

        [Command("add project")]
        [RequireBotPermission(GuildPermission.ManageChannels & GuildPermission.ManageRoles)]
        public async Task AddProject([Remainder]string projectName)
        {
            ICategoryChannel ProjectsCategory = CommandHelper.FindCategory(Context.Guild.CategoryChannels, Strings.ProjectCategoryName);
            string TrueName = projectName.Replace(' ', '-');

            if (ProjectsCategory is null)
            {
                //Create the category
                ProjectsCategory = await Context.Guild.CreateCategoryChannelAsync(Strings.ProjectCategoryName);
            }

            ITextChannel ProjectChannel = await Context.Guild.CreateTextChannelAsync(TrueName);
            
            await ProjectChannel.ModifyAsync(delegate (TextChannelProperties ac) { ac.CategoryId = ProjectsCategory.Id; });

            //fully order channels in category
            List<ITextChannel> channels = new List<ITextChannel>(Context.Guild.TextChannels);

            channels.RemoveAll(x => x.CategoryId != ProjectsCategory.Id);
            channels.Add(ProjectChannel);

            await CommandHelper.OrderChannels(channels, ProjectsCategory.Id);

            //create a role
            IRole ProjectManagerRole = await Context.Guild.CreateRoleAsync($"{TrueName}-Manager");
            await ProjectManagerRole.ModifyAsync(delegate (RoleProperties rp) { rp.Mentionable = true; });

            IRole ProjectRole = await Context.Guild.CreateRoleAsync(TrueName);
            await ProjectRole.ModifyAsync(delegate (RoleProperties rp) { rp.Mentionable = true; });

            await ((IGuildUser)Context.User).AddRolesAsync(new List<IRole> { ProjectManagerRole, ProjectRole });

            await ProjectChannel.AddPermissionOverwriteAsync(ProjectManagerRole, new OverwritePermissions(manageMessages: PermValue.Allow));

            await ReplyAsync($"Created {ProjectChannel.Mention} with role {ProjectRole.Mention}!");
        }

        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.ManageChannels & GuildPermission.ManageRoles)]
        [Command("archive")]
        public async Task ArchiveProject()
        {
            ICategoryChannel ProjectsCategory = CommandHelper.FindCategory(Context.Guild.CategoryChannels, Strings.ProjectCategoryName);
            ICategoryChannel ArchiveCategory = CommandHelper.FindCategory(Context.Guild.CategoryChannels, Strings.ArchiveCategoryName);

            if (((ITextChannel)Context.Channel).CategoryId != ProjectsCategory.Id)
            {
                await ReplyAsync("This channel is not a project channel!");
                return;
            }

            if (ArchiveCategory is null) //Create the category
            {
                ArchiveCategory = await Context.Guild.CreateCategoryChannelAsync(Strings.ArchiveCategoryName);
                await ArchiveCategory.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new OverwritePermissions(sendMessages: PermValue.Deny));
            }

            await ((IGuildChannel)Context.Channel).ModifyAsync(delegate (GuildChannelProperties ac) { ac.CategoryId = ArchiveCategory.Id; });

            List<ITextChannel> channels = new List<ITextChannel>(Context.Guild.TextChannels);
            channels.RemoveAll(x => x.CategoryId != ArchiveCategory.Id);
            channels.Add((ITextChannel)Context.Channel);
            
            await CommandHelper.OrderChannels(channels, ArchiveCategory.Id);

            IRole ProjectRole = CommandHelper.FindRole(Context.Guild.Roles, Context.Channel.Name);
            IRole ProjectManagerRole = CommandHelper.FindRole(Context.Guild.Roles, $"{Context.Channel.Name}-Manager");

            await Context.Guild.GetRole(ProjectRole.Id).DeleteAsync();
            await Context.Guild.GetRole(ProjectManagerRole.Id).DeleteAsync();
            await ReplyAsync("Archived project and cleared role!");
            await ((IGuildChannel)Context.Channel).AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, (OverwritePermissions)ArchiveCategory.GetPermissionOverwrite(Context.Guild.EveryoneRole));
        }

        [Command("role")]
        public async Task RoleProject()
        {
            ICategoryChannel ProjectsCategory = CommandHelper.FindCategory(Context.Guild.CategoryChannels, Strings.ProjectCategoryName);
            if (((ITextChannel)Context.Channel).CategoryId != ProjectsCategory.Id)
            {
                await ReplyAsync("This channel is not a project channel - please enter a project name or run this command in a project channel!");
                return;
            }

            await RoleProject(Context.Channel.Name);
        }
        [Command("role")]
        public async Task RoleProject([Remainder]string projectName)
        {
            string TrueName = projectName.Replace(' ', '-');
            ITextChannel ProjectChannel = (ITextChannel)Context.Guild.Channels.First(x => x.Name == TrueName);

            ICategoryChannel ProjectsCategory = CommandHelper.FindCategory(Context.Guild.CategoryChannels, Strings.ProjectCategoryName);
            if (ProjectChannel.CategoryId != ProjectsCategory.Id)
            {
                await ReplyAsync("The named project was not found!");
                return;
            }

            IRole role = Context.Guild.Roles.First(x => x.Name.ToLower() == TrueName.ToLower());
            IGuildUser GuildUser = (IGuildUser)Context.User;
            if (GuildUser.RoleIds.Contains(role.Id))
            {
                await GuildUser.RemoveRoleAsync(role);
                await ReplyAsync($"Removed the {role.Name} role from {GuildUser.Mention}!");
            }
            else
            {
                await GuildUser.AddRoleAsync(role);
                await ReplyAsync($"Added the {role.Name} role to {GuildUser.Mention}!");
            }
        }

        
    }

    public class CommandHelper
    {
        public static ICategoryChannel FindCategory(IReadOnlyCollection<SocketCategoryChannel> CategoryList, string categoryName)
        {
            foreach (SocketCategoryChannel s in CategoryList)
            {
                if(s.Name.ToLower() == categoryName.ToLower())
                {
                    return s; 
                }
            }
            return null;
        }
        public static ITextChannel FindChannel(IReadOnlyCollection<SocketTextChannel> ChannelList, string channelName)
        {
            foreach (SocketTextChannel s in ChannelList)
            {
                if (s.Name.ToLower() == channelName.ToLower())
                {
                    return s;
                }
            }
            return null;
        }
        public static IRole FindRole(IReadOnlyCollection<SocketRole> RoleList, string RoleName)
        {
            foreach (SocketRole r in RoleList)
            {
                if (r.Name.ToLower() == RoleName.ToLower())
                {
                    return r;
                }
            }
            return null;
        }

        public static async Task OrderChannels(List<ITextChannel> channelList, ulong categoryID)
        {
            int order = 0;

            foreach (ITextChannel chan in channelList.OrderBy(x => x.Name))
            {
                await chan.ModifyAsync(delegate (TextChannelProperties ac) { ac.Position = order; });
                order++;
            }
            await Task.CompletedTask;
        }
    }
}
