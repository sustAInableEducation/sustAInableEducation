﻿using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace sustAInableEducation_backend.Models
{
    public class Story
    {
        [JsonIgnore]
        public Guid Id { get; set; }

        public ICollection<StoryPart> Parts { get; set; } = new List<StoryPart>();
        public StoryResult? Result { get; set; }

        [MaxLength(256)]
        public string? Title { get; set; }
        public string Prompt { get; set; } = null!;
        public uint Length { get; set; }
        public float Temperature { get; set; }
        public float TopP { get; set; }
        public float TotalImpact { get; set; } = 0;

        [JsonIgnore]
        public bool IsComplete => Parts.Count >= Length;
    }

    public class StoryResult
    {
        public string Text { get; set; } = null!;
        public string Summary { get; set; } = null!;
        public string[] PositiveChoices { get; set; } = [];
        public string[] NegativeChoices { get; set; } = [];
        public string[] Learnings { get; set; } = [];
        public string[] DiscussionQuestions { get; set; } = [];
    }

    public class StoryRequest
    {
        public string Prompt { get; set; } = null!;
        public uint Length { get; set; }
        public float Temperature { get; set; }
        public float TopP { get; set; }
    }
}
