using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SignalRSwaggerGen.Attributes;
using sustAInableEducation_backend.Models;
using sustAInableEducation_backend.Repository;
using System.Security.Claims;

namespace sustAInableEducation_backend.Hubs
{
    public static class MessageType
    {
        public const string UserJoined = "UserJoined";
        public const string UserLeft = "UserLeft";
        public const string PartGenerating = "PartGenerating";
        public const string PartGenerated = "PartGenerated";
        public const string ImageGenerated = "ImageGenerated";
        public const string ResultGenerated = "ResultGenerated";
        public const string VotingStarted = "VotingStarted";
        public const string VotingUpdated = "VotingUpdated";
        public const string ChoiceSet = "ChoiceSet";
        public const string AIErrorOccured = "ErrorOccured";
    }

    [SignalRHub("spaceHub/{spaceId}", tag: "(WebSocket) SpaceHub")]
    [Authorize]
    public class SpaceHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly string _userId;
        private readonly Guid _spaceId;
        private readonly IAIService _ai;

        public SpaceHub(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, IAIService ai)
        {
            _context = context;
            _userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            Guid.TryParse(httpContextAccessor.HttpContext?.GetRouteValue("id") as string, out _spaceId);

            _ai = ai;
        }

        [SignalRHidden]
        public override async Task OnConnectedAsync()
        {
            if (!await _context.IsParticipant(_userId, _spaceId))
            {
                throw new HubException("Unauthorized");
            }

            await Groups.AddToGroupAsync(Context!.ConnectionId, _spaceId.ToString());
            var participtant = _context.SpaceParticipant.Find(_spaceId, _userId)!;
            participtant.IsOnline = true;
            await _context.SaveChangesAsync();

            await SendMessage(MessageType.UserJoined, participtant);
            await base.OnConnectedAsync();
        }

        [SignalRHidden]
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _context.SpaceParticipant.Find(_spaceId, _userId)!.IsOnline = false;
            await _context.SaveChangesAsync();

            await SendMessage(MessageType.UserLeft, _userId);
            await base.OnDisconnectedAsync(exception);
        }

        private async Task SendMessage(string type, object? arg1 = null, object? message2 = null)
        {
            if (message2 != null)
            {
                await Clients.Group(_spaceId.ToString()).SendAsync(type, arg1, message2);
            }
            else
            {
                await Clients.Group(_spaceId.ToString()).SendAsync(type, arg1);
            }
        }

        public async Task GeneratePart()
        {
            if (!await _context.IsHost(_userId, _spaceId))
            {
                throw new HubException("Unauthorized");
            }
            var space = (await _context.SpaceWithStory.FirstOrDefaultAsync(e => e.Id == _spaceId))!;
            var story = space.Story;
            if (story.Result != null)
            {
                throw new HubException("Story is already complete");
            }
            var lastPart = story.Parts.LastOrDefault();
            if (!story.IsComplete && lastPart != null && lastPart.ChosenNumber == null)
            {
                throw new HubException("Choice not set");
            }

            await SendMessage(MessageType.PartGenerating);

            try
            {
                if (story.Parts.Count == 0)
                {
                    var storyStart = await _ai.StartStory(story);
                    story.Parts.Add(storyStart.Item1);
                    story.Title = storyStart.Item2;
                }
                else if (story.IsComplete)
                {
                    story.Result = await _ai.GenerateResult(story);
                }
                else
                {
                    story.Parts.Add(await _ai.GenerateNextPart(story));
                }
            }
            catch(AIException e)
            {
                await SendMessage(MessageType.AIErrorOccured, e.Message);
                throw new HubException("AI error");
            }

            await _context.SaveChangesAsync();

            if (story.Result != null)
            {
                await SendMessage(MessageType.ResultGenerated, story.Result);
                if (space.IsImageGenerationEnabled)
                {
                    story.Result!.Image = await _ai.GenerateStoryImage(story);
                    await SendMessage(MessageType.ImageGenerated, story.Result!.Image);
                }
            }
            else
            {
                await SendMessage(MessageType.PartGenerated, story.Parts.Last());
                if (space.IsImageGenerationEnabled)
                {
                    story.Parts.Last().Image = await _ai.GenerateStoryImage(story);
                    await SendMessage(MessageType.ImageGenerated, story.Parts.Last().Image);
                }
            }
        }

        public async Task StartVoting()
        {
            if (!await _context.IsHost(_userId, _spaceId))
            {
                throw new HubException("Unauthorized");
            }
            var space = (await _context.SpaceWithStory.FirstOrDefaultAsync(e => e.Id == _spaceId))!;
            if (space.Story.Result != null)
            {
                throw new HubException("Story is complete");
            }
            var part = space.Story.Parts.LastOrDefault();
            if (part == null)
            {
                throw new HubException("No part")
;           }
            if (part.IsVotingActive)
            {
                throw new HubException("Voting is already active");
            }
            
            _context.SpaceParticipant.Where(p => p.SpaceId == _spaceId)
                .ExecuteUpdate(p => p
                    .SetProperty(x => x.VoteImpact, x => null));
            foreach (var choice in part.Choices)
            {
                choice.NumberVotes = 0;
            }
            part.VotingEndAt = DateTime.Now.AddSeconds(space.VotingTimeSeconds);
            await _context.SaveChangesAsync();
            await SendMessage(MessageType.VotingStarted, part.VotingEndAt);
        }

        public async Task Vote(uint number)
        {
            var part = (await _context.SpaceWithStory.FirstOrDefaultAsync(e => e.Id == _spaceId))!.Story.Parts.LastOrDefault();
            if (part == null)
            {
                throw new HubException("No part");
            }
            if (!part.IsVotingActive)
            {
                throw new HubException("Voting is not active");
            }
            var participant = (await _context.SpaceParticipant.FindAsync(_spaceId, _userId))!;
            if (participant.VoteImpact != null)
            {
                throw new HubException("Already voted");
            }
            var choice = part.Choices.First(c => c.Number == number);
            choice.NumberVotes++;
            participant.VoteImpact = choice.Impact;
            await _context.SaveChangesAsync();
            await SendMessage(MessageType.VotingUpdated, part.Choices);
        }

        public async Task SetChoice(uint number)
        {
            if (!await _context.IsHost(_userId, _spaceId))
            {
                throw new HubException("Unauthorized");
            }
            var story = (await _context.SpaceWithStory.FirstOrDefaultAsync(e => e.Id == _spaceId))!.Story;
            var part = story.Parts.LastOrDefault();
            if (part == null)
            {
                throw new HubException("No part");
            }
            if (part.ChosenNumber != null)
            {
                throw new HubException("Already chosen");
            }
            var choice = part.Choices.First(c => c.Number == number);
            if (choice == null)
            {
                throw new HubException("Invalid choice");
            }
            part.ChosenNumber = number;
            story.TotalImpact += choice.Impact;
            _context.SpaceParticipant.Where(p => p.SpaceId == _spaceId && p.VoteImpact != null)
                .ExecuteUpdate(p => p
                    .SetProperty(x => x.Impact, x => x.Impact + x.VoteImpact)
                    .SetProperty(x => x.VoteImpact, x => null));
            await _context.SaveChangesAsync();
            await SendMessage(MessageType.ChoiceSet, number, story.TotalImpact);
        }
    }
}
