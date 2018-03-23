using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SPExplorer.ReportSamples
{
    public class SiteSizeReport
    {
        TraceSource Trace = new TraceSource("SpExplorer");

        List<ListInfo> listFolder = new List<ListInfo>();

        void ProcWeb(Web web)
        {
            var lists = web.Lists;
            web.Context.Load(lists, i => i.Where(j => j.Hidden == false && j.BaseType == BaseType.DocumentLibrary),
                i => i.Include(k => k.ItemCount, k => k.LastItemDeletedDate, k => k.LastItemModifiedDate));
            web.Context.Load(web, i => i.ServerRelativeUrl);
            web.Context.ExecuteQuery();

            Trace.TraceEvent(TraceEventType.Information, 2, web.ServerRelativeUrl);

            foreach (var list in lists)
            {
                try
                {
                    var folder = list.RootFolder;
                    web.Context.Load(folder, i => i.ServerRelativeUrl);
                    web.Context.ExecuteQuery();

                    Trace.TraceEvent(TraceEventType.Verbose, 4, folder.ServerRelativeUrl);
                    if (done.ContainsKey(folder.ServerRelativeUrl))
                        continue;

                    var kbs = ProcFolder(folder);

                    var listInfo = new ListInfo()
                    {
                        Site = web.ServerRelativeUrl,
                        List = folder.ServerRelativeUrl,
                        ItemCount = list.ItemCount,
                        KBSize = kbs,
                        LastActivity = (list.LastItemModifiedDate > list.LastItemDeletedDate) ? list.LastItemModifiedDate : list.LastItemDeletedDate
                    };

                    Trace.TraceEvent(TraceEventType.Information, 3, listInfo.List);

                    listFolder.Add(listInfo);
                }
                catch (ServerUnauthorizedAccessException ex)
                {
                    Trace.TraceEvent(TraceEventType.Error, 4, ex.ToString());
                    continue;

                }
            }

            try
            {
                web.Context.Load(web.Webs, i => i.Include(k => k.ServerRelativeUrl));
                web.Context.ExecuteQuery();
            }
            catch (ServerUnauthorizedAccessException ex)
            {
                Trace.TraceEvent(TraceEventType.Error, 3, ex.ToString());
                return;

            }

            foreach (var subweb in web.Webs)
            {
                ProcWeb(subweb);
            }
        }

        public List<ListInfo> SiteSize(ClientContext ctx, List<ListInfo> state)
        {

            listFolder = state;

            try
            {
                ProcWeb(ctx.Web);
            }
            catch (Exception ex)
            {
                Trace.TraceEvent(TraceEventType.Error, 1, ex.ToString());
                throw;
            }

            return listFolder;
        }

        SortedDictionary<string, string> done = new SortedDictionary<string, string>();

        static long ProcFolder(Folder folder)
        {
            var fiels = folder.Files;
            folder.Context.Load(fiels, j => j.Include(k => k.Length, k => k.Name));

            folder.Context.ExecuteQuery();

            long kns = 0;
            foreach (var file in fiels)
            {
                var length = file.Length;
                kns += length;
            }

            folder.Context.Load(folder.Folders);
            folder.Context.ExecuteQuery();

            foreach (var sub in folder.Folders)
            {
                kns += ProcFolder(sub);
            }

            return kns;
        }

        public class ListInfo
        {
            public string Site;
            public string List;
            public int ItemCount = 0;
            public long KBSize = 0;
            public DateTime LastActivity;
        }
    }
}
