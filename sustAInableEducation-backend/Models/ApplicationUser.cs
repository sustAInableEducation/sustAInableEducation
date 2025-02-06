using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using sustAInableEducation_backend.Repository;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace sustAInableEducation_backend.Models
{
    public class ApplicationUser : IdentityUser
    {
        [JsonIgnore]
        public ICollection<SpaceParticipant> Participations { get; set; } = new List<SpaceParticipant>();

        public string AnonUserName { get; set; } = UserNameGenService.GenerateUserName();
        public string? ProfileImage { get; set; }
    }

    public class ChangeEmailRequest
    {
        public string NewEmail { get; set; } = null!;
    }

    public class ChangePasswordRequest
    {
        public string OldPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }

    public enum ImageStyle
    {
        [EnumMember(Value = "Cartoon")]
        Cartoon,

        [EnumMember(Value = "Pop-Art")]
        PopArt,

        [EnumMember(Value = "PixelArt")]
        PixelArt,

        [EnumMember(Value = "FantasyArt")]
        FantasyArt,

        [EnumMember(Value = "Stencil")]
        Stencil,

        [EnumMember(Value = "Papercraft")]
        Papercraft,

        [EnumMember(Value = "Risograph")]
        Risograph,

        [EnumMember(Value = "Cyberpunk")]
        Cyberpunk,

        [EnumMember(Value = "PencilSketch")]
        PencilSketch,

        [EnumMember(Value = "PaperCollage")]
        PaperCollage,

        [EnumMember(Value = "Psychedelic")]
        Psychedelic,

        [EnumMember(Value = "StreetArt")]
        StreetArt,

        [EnumMember(Value = "Ukiyo-e")]
        UkiyoE,

        [EnumMember(Value = "Manga")]
        Manga,

        [EnumMember(Value = "Medieval")]
        Medieval
    }

}
