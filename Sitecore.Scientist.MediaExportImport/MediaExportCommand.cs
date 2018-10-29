﻿using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.IO;
using Sitecore.Shell.Applications.Dialogs.ProgressBoxes;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Web;

namespace Sitecore.Scientist.MediaExportImport
{
    public class MediaExportCommand : Command
    {
        public MediaExportCommand()
        {
        }
        public override void Execute(CommandContext context)
        {
            if ((int)context.Items.Length == 1)
            {
                Item items = context.Items[0];
                NameValueCollection nameValueCollection = new NameValueCollection();
                nameValueCollection["uri"] = items.Uri.ToString();
                Context.ClientPage.Start(this, "Run", nameValueCollection);
            }
        }

        private bool extractRecursiveParam(string[] paramsArray)
        {
            bool flag;
            if ((int)paramsArray.Length <= 1)
            {
                flag = true;
            }
            else if (!bool.TryParse(paramsArray[1], out flag))
            {
                flag = true;
            }
            return flag;
        }

        public override CommandState QueryState(CommandContext context)
        {
            Error.AssertObject(context, "context");
            if ((int)context.Items.Length != 1)
            {
                return CommandState.Hidden;
            }
            if (context.Items[0] == null)
            {
                return CommandState.Hidden;
            }
            return base.QueryState(context);
        }

        protected virtual void Run(ClientPipelineArgs args)
        {
            ItemUri itemUri = ItemUri.Parse(args.Parameters["uri"]);
            Item item = Database.GetItem(itemUri);
            Error.AssertItemFound(item);
            bool flag = true;
            string str1 = HttpContext.Current.Server.MapPath("~/") + string.Concat(Settings.DataFolder.TrimStart(new char[] { '/' }), "\\", Settings.GetSetting("Sitecore.Scientist.MediaExportImport.ExportFolderName", "MediaExports"));
            str1 = str1.Replace("/", "\\");
            FileUtil.CreateFolder(FileUtil.MapPath(str1));
            var innerfolders = item.Paths.FullPath.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var folder in innerfolders)
            {
                str1 = str1 + "\\" + folder;
                FileUtil.CreateFolder(FileUtil.MapPath(str1));
            }
            Log.Info(string.Concat("Starting export of media items to: ", string.Concat(Settings.DataFolder.TrimStart(new char[] { '/' }), "\\", Settings.GetSetting("Sitecore.Scientist.MediaExportImport.ExportFolderName", "MediaExports"))), this);
            ProgressBoxMethod progressBoxMethod = new ProgressBoxMethod(StartProcess);
            object[] objArray = new object[] { item, str1, flag };
            ProgressBox.Execute("Export Media Items...", "Export Media Items", progressBoxMethod, objArray);
        }

        public void StartProcess(params object[] parameters)
        {
            Item item = (Item)parameters[0];
            string str = (string)parameters[1];
            bool flag = (bool)parameters[2];
            MediaExporter mediaExporter = new MediaExporter(str);
            foreach (Item child in item.GetChildren())
            {
                mediaExporter.ProcessMediaItems(child, flag);
            }
            string[] files = Directory.GetFiles(str);
            foreach (var file in files)
            {
                Context.Job.Status.Messages.Add(string.Concat("Cleaning: ", file));
                mediaExporter.CleanMediaFiles(item, file);
            }
            string[] folders = Directory.GetDirectories(str);
            foreach (var folder in folders)
            {
                Context.Job.Status.Messages.Add(string.Concat("Cleaning: ", folder));
                mediaExporter.CleanMediaFiles(item, folder);
            }
        }
    }
}
