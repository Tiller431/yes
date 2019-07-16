using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using opi.v1;
using osu.Game.Beatmaps;
using Sora.Helpers;
using Sora.Interfaces;

namespace Pisstaube.Database.Models
{
    [Serializable]
    public class ChildrenBeatmap : ISerializer
    {
        [Key]
        [Required]
        [JsonProperty("BeatmapID")]
        public int BeatmapId { get; set; }

        [JsonProperty("ParentSetID")]
        public int ParentSetId { get; set; }
        
        [JsonIgnore]
        public BeatmapSet Parent { get; set; } 

        [JsonProperty("DiffName")]
        public string DiffName { get; set; }

        [JsonProperty("FileMD5")]
        public string FileMd5 { get; set; }

        [JsonProperty("Mode")]
        public PlayMode Mode { get; set; }

        [JsonProperty("BPM")]
        public double Bpm { get; set; }

        [JsonProperty("AR")]
        public float Ar { get; set; }

        [JsonProperty("OD")]
        public float Od { get; set; }

        [JsonProperty("CS")]
        public float Cs { get; set; }

        [JsonProperty("HP")]
        public float Hp { get; set; }

        [JsonProperty("TotalLength")]
        public int TotalLength { get; set; }

        [JsonProperty("HitLength")]
        public long HitLength { get; set; }

        [JsonProperty("Playcount")]
        public int Playcount { get; set; }

        [JsonProperty("Passcount")]
        public int Passcount { get; set; }

        [JsonProperty("MaxCombo")]
        public long MaxCombo { get; set; }

        [JsonProperty("DifficultyRating")]
        public double DifficultyRating { get; set; }

        public static ChildrenBeatmap FromBeatmapInfo(BeatmapInfo info, BeatmapSetOnlineInfo setOnlineInfo, BeatmapSet parent = null)
        {
            if (info == null)
                return null;

            var cb = new ChildrenBeatmap
            {
                BeatmapId = info.OnlineBeatmapID ?? -1,
                ParentSetId = info.BeatmapSetInfoID,
                Parent = parent,
                DiffName = info.Version,
                FileMd5 = info.MD5Hash,
                Mode = (PlayMode) info.RulesetID,
                Ar = info.BaseDifficulty.ApproachRate,
                Od = info.BaseDifficulty.OverallDifficulty,
                Cs = info.BaseDifficulty.CircleSize,
                Hp = info.BaseDifficulty.DrainRate,
                TotalLength = (int) info.OnlineInfo.Length,
                HitLength = (int) info.StackLeniency,
                Playcount = info.OnlineInfo.PassCount,
                Bpm = setOnlineInfo.BPM,
                MaxCombo = info.OnlineInfo.CircleCount,
                DifficultyRating = info.StarDifficulty
            };
            
            return cb;
        }

        public string ToDirect() => $"{DiffName.Replace("@", "")} " +
                                    $"({Math.Round(DifficultyRating, 2)}★~" +
                                    $"{Bpm}♫~AR" +
                                    $"{Ar}~OD" +
                                    $"{Od}~CS" +
                                    $"{Cs}~HP" +
                                    $"{Hp}~" +
                                    $"{(int)MathF.Floor(TotalLength) / 60}m" +
                                    $"{TotalLength % 60}s)@" +
                                    $"{(int)Mode},";

        public void ReadFromStream(MStreamReader sr)
        {
            BeatmapId = sr.ReadInt32();
            ParentSetId = sr.ReadInt32();
            DiffName = sr.ReadString();
            FileMd5 = sr.ReadString();
            Mode = (PlayMode) sr.ReadSByte();
            Bpm = sr.ReadInt32();
            Ar = sr.ReadSingle();
            Od = sr.ReadSingle();
            Cs = sr.ReadSingle();
            Hp = sr.ReadSingle();
            TotalLength = sr.ReadInt32();
            Playcount = sr.ReadInt32();
            Passcount = sr.ReadInt32();
            MaxCombo = sr.ReadInt64();
            DifficultyRating = sr.ReadDouble();
        }

        public void WriteToStream(MStreamWriter sw)
        {
            sw.Write(BeatmapId);
            sw.Write(ParentSetId);
            sw.Write(DiffName, true);
            sw.Write(FileMd5, true);
            sw.Write((sbyte) Mode);
            sw.Write(Bpm);
            sw.Write(Ar);
            sw.Write(Od);
            sw.Write(Cs);
            sw.Write(Hp);
            sw.Write(TotalLength);
            sw.Write(Playcount);
            sw.Write(Passcount);
            sw.Write(MaxCombo);
            sw.Write(DifficultyRating);
        }
    }
}