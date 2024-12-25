﻿using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace sustAInableEducation_backend.Models
{
    public class Space
    {
        public Guid Id { get; set; }
        [JsonIgnore]
        public Guid StoryId { get; set; }

        public ICollection<SpaceParticipant> Participants { get; set; } = new List<SpaceParticipant>();
        public Story Story { get; set; } = null!;
        [JsonIgnore]
        public SpaceAccessCode? AccessCode { get; set; }
        public uint VotingTimeSeconds { get; set; } = 30;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class SpaceRequest
    {
        public StoryRequest Story { get; set; }
        public uint VotingTimeSeconds { get; set; } = 30;
    }
}