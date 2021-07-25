using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.DataModels
{
    public partial class DiscordApiPing
    {
        public Period Period { get; set; }
        public List<MetricElement> Metrics { get; set; }
        public DiscordApiPingSummary Summary { get; set; }
    }

    public partial class MetricElement
    {
        public MetricMetric Metric { get; set; }
        public MetricSummary Summary { get; set; }
        public List<Datum> Data { get; set; }
    }

    public partial class Datum
    {
        public long Timestamp { get; set; }
        public long Value { get; set; }
    }

    public partial class MetricMetric
    {
        public string Name { get; set; }
        public string MetricIdentifier { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public string Id { get; set; }
        public string MetricsProviderId { get; set; }
        public string MetricsDisplayId { get; set; }
        public DateTimeOffset MostRecentDataAt { get; set; }
        public bool Backfilled { get; set; }
        public DateTimeOffset LastFetchedAt { get; set; }
        public long BackfillPercentage { get; set; }
    }

    public partial class MetricSummary
    {
        public double Sum { get; set; }
        public double Mean { get; set; }
    }

    public partial class Period
    {
        public long Count { get; set; }
        public long Interval { get; set; }
        public string Identifier { get; set; }
    }

    public partial class DiscordApiPingSummary
    {
        public double Sum { get; set; }
        public double Mean { get; set; }
        public long Last { get; set; }
    }
}
