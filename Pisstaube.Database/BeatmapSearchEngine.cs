using System;
using System.Collections.Generic;
using System.Linq;
using Amib.Threading;
using Elasticsearch.Net;
using Nest;
using opi.v1;
using osu.Framework.Logging;
using osu.Game.Beatmaps;
using Pisstaube.Database.Models;
using LogLevel = osu.Framework.Logging.LogLevel;

namespace Pisstaube.Database
{
    public class BeatmapSearchEngine
    {
        private readonly PisstaubeDbContextFactory _contextFactory;
        private readonly ElasticClient _elasticClient;
        private readonly SmartThreadPool _pool;
        
        public BeatmapSearchEngine(PisstaubeDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
            _pool = new SmartThreadPool();
            _pool.MaxThreads = Environment.ProcessorCount * 16;
            _pool.Start();

            var settings = new ConnectionSettings(
                    new Uri(
                        $"http://{Environment.GetEnvironmentVariable("ELASTIC_HOSTNAME")}:{Environment.GetEnvironmentVariable("ELASTIC_PORT")}"
                    )
                )
                .DefaultIndex("pisstaube");

            _elasticClient = new ElasticClient(settings);
            _elasticClient.CreateIndex("pisstaube");
        }
        
        private static int xi;
        private void rIndexBeatmap(BeatmapSet set)
        {
            if (xi++ % 10000 == 0)
                Logger.LogPrint($"Index ElasticBeatmap of Id {set.SetId}");
                    
            _elasticClient.DeleteByQuery<ElasticBeatmap>(
                x =>
                    x.Query(query => query.Exists(exists => exists.Field(field => field.Id == set.SetId)))
            );
            var map = ElasticBeatmap.GetElasticBeatmap(set);

            //Logger.LogPrint($"Index ElasticBeatmap of Id {set.SetId}");

            var result = _elasticClient.IndexDocument(map);
            if (!result.IsValid)
                Logger.LogPrint(result.DebugInformation, LoggingTarget.Network, LogLevel.Important);
        }
        public void IndexBeatmap(BeatmapSet set)
        {
            _pool.QueueWorkItem(rIndexBeatmap, set);
        }

        public void IndexBeatmapSets(IEnumerable<BeatmapSet> sets)
        {
            foreach (var set in sets)
            {
                IndexBeatmap(set);
            }
        }

        public void DeleteAllBeatmaps()
        {
            Logger.LogPrint("Deleting all Beatmaps from ElasticSearch!");
            _elasticClient.DeleteByQuery<ElasticBeatmap>(x => x.MatchAll());
        }

        public void DeleteBeatmap(int setId)
        {
            _elasticClient.DeleteByQuery<ElasticBeatmap>(x => x.Query(q =>
                q.Bool(b =>
                    b.Must(m =>
                        m.Term(t =>
                            t.Field(bm => bm.Id).Value(setId)
                            )
                        )
                    )
                )
            );
        }

        public List<BeatmapSet> Search(string query,
            int amount = 100,
            int offset = 0,
            BeatmapSetOnlineStatus? rankedStatus = null,
            PlayMode mode = PlayMode.All)
        {
            if (amount > 100 || amount <= -1)
                amount = 100;
            
            var sets = new List<BeatmapSet>();
            
            if (!string.IsNullOrWhiteSpace(query))
            { 
                var result =_elasticClient.Search<ElasticBeatmap>(s =>
                {
                    var ret = s
                        .From(offset)
                        .Size(amount)
                        .MinScore(10)
                        .Query(q =>
                            q.Bool(b => b
                                    .Must(must =>
                                        {
                                            QueryContainer res = must;
                                            if (rankedStatus != null)
                                                res = must.Term(term => term.Field(p => p.RankedStatus)
                                                    .Value(rankedStatus));
                                            
                                            if (mode != PlayMode.All)
                                                res &= must.Term(term => term.Field(p => p.Mode)
                                                    .Value(mode));
                                            
                                            return res;
                                        }
                                    )
                                    .Should(should =>
                                        should.Match(match => match.Field(p => p.Creator).Query(query).Boost(5)) ||
                                        should.Match(match => match.Field(p => p.Artist).Query(query)) ||
                                        should.Match(match => match.Field(p => p.DiffName).Query(query)) ||
                                        should.Match(match => match.Field(p => p.Tags).Query(query)) ||
                                        should.Match(match => match.Field(p => p.Title).Query(query).Boost(2))
                                    )
                            )
                        );
                    Logger.LogPrint(_elasticClient.RequestResponseSerializer.SerializeToString(ret),
                        LoggingTarget.Network, LogLevel.Debug);
                    return ret;
                });

                if (!result.IsValid)
                {
                    Logger.LogPrint(result.DebugInformation, LoggingTarget.Network, LogLevel.Important);
                    return null;
                }
                
                var r = result.Hits.Select(
                    hit => hit != null ? _contextFactory.Get().BeatmapSet.FirstOrDefault(set => set.SetId == hit.Source.Id) : null
                );
                sets.AddRange(r);

                var newSets = new List<BeatmapSet>();
                
                foreach (var s in sets)
                {
                    if (s == null)
                        continue;
                    
                    newSets.Add(s);
                    if (s.ChildrenBeatmaps == null)
                        s.ChildrenBeatmaps = _contextFactory.Get().Beatmaps.Where(cb => cb.ParentSetId == s.SetId).ToList();
                }

                sets = newSets;
            }
            else
            {
                var sSets = _contextFactory.Get().BeatmapSet
                                           .Where(
                                               set => (
                                                      rankedStatus == null || set.RankedStatus == rankedStatus) &&
                                                      _contextFactory.Get().Beatmaps.Where(
                                                                         cb => cb.ParentSetId == set.SetId
                                                                     )
                                                                     .FirstOrDefault(
                                                                         cb => mode == PlayMode.All || cb.Mode == mode
                                                                     ) != null
                                                )
                                           .OrderByDescending(x => x.ApprovedDate)
                                           .Skip(offset)
                                           .Take(amount);

                foreach (var s in sSets)
                {
                    if (s.ChildrenBeatmaps == null)
                        s.ChildrenBeatmaps = _contextFactory.Get().Beatmaps.Where(cb => cb.ParentSetId == s.SetId).ToList();
                }

                sets = sSets.ToList();
            }

            foreach (var s in sets)
            {
                // Fixes an Issue where osu!direct may not load!
                s.Artist = s.Artist.Replace("|", "");
                s.Title = s.Title.Replace("|", "");
                s.Creator = s.Creator.Replace("|", "");

                foreach (var bm in s.ChildrenBeatmaps)
                    bm.DiffName =  bm.DiffName.Replace("|", "");
            }

            return sets;
        }
    }
}