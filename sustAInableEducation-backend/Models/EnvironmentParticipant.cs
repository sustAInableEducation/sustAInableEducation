﻿using System.Text.Json.Serialization;

namespace sustAInableEducation_backend.Models
{
    public class EnvironmentParticipant
    {
        public string UserId { get; set; } = null!;
        [JsonIgnore]
        public Guid EnvironmentId { get; set; }

        [JsonIgnore]
        public ApplicationUser User { get; set; } = null!;
        [JsonIgnore]
        public Environment Environment { get; set; } = null!;

        public string UserName => User?.AnonUserName ?? "";
        public bool IsHost { get; set; }
        public bool IsOnline { get; set; }
        [JsonIgnore]
        public float? VoteImpact { get; set; }
        public float Impact { get; set; } = 0;
    }
}