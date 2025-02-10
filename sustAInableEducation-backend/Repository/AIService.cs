
using System.Runtime.Serialization;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SkiaSharp;
using sustAInableEducation_backend.Models;

namespace sustAInableEducation_backend.Repository
{
    public class AIService : IAIService
    {
        // Benjamin Edlinger
        private readonly IConfiguration _config;
        private readonly ILogger _logger;
        private readonly HttpClient _client;
        const int MAX_RETRY_ATTEMPTS = 2; // Maximum number of retry attempts for a failed request or deserialization

        // Benjamin Edlinger
        public AIService(IConfiguration config, ILogger<AIService> logger)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(logger);
            _config = config;
            _logger = logger;
            try
            {
                _client = new HttpClient
                {
                    BaseAddress = new Uri(_config["DeepInfra:Url"] ?? throw new ArgumentNullException("DeepInfra:Url configuration is missing")),
                    Timeout = TimeSpan.FromMinutes(5)
                };
                _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config["DeepInfra:ApiKey"] ?? throw new ArgumentNullException("DeepInfra:ApiKey configuration is missing")}");
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to initialise AI service: {Exception}", e);
                throw;
            }
            _logger.LogInformation("AI service initialised");
        }

        // Benjamin Edlinger
        /// <summary>
        /// Starts a new story with the given story object
        /// </summary>
        /// <param name="story"> The story object to start</param>
        /// <returns>The first part of the story and the title of the story</returns>
        /// <exception cref="ArgumentException">If the story object is invalid</exception>
        /// <exception cref="AIException">If the story could not be started due to an error while fetching the content or deserializing</exception>
        public async Task<(StoryPart, string)> StartStory(Story story)
        {
            ArgumentNullException.ThrowIfNull(story);

            _logger.LogInformation("Starting new story with title {Title}", story.Title);
            List<ChatMessage> chatMessages;
            try
            {
                chatMessages = RebuildChatMessagesStory(story);
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to rebuild chat messages because of error in story object: {Exception}", e);
                throw new ArgumentException("Failed to rebuild chat messages because of error in story object", e);
            }

            string assistantContent = null!;
            int attempt = 0;
            while (attempt < MAX_RETRY_ATTEMPTS)
            {
                try
                {
                    assistantContent = await FetchAssitantContent(chatMessages, story.Temperature, story.TopP);
                    break;
                }
                catch (Exception e)
                {
                    _logger.LogError("Failed to fetch the assistant content for story part: {Exception}", e);
                    if (attempt >= MAX_RETRY_ATTEMPTS - 1)
                    {
                        _logger.LogError("Reached maximum retry attempts for trying to fetch the assistant content for story part");
                        throw new AIException("Failed to fetch the assistant content for story part", e);
                    }
                    attempt++;
                }
            }

            attempt = 0;
            while (attempt < MAX_RETRY_ATTEMPTS)
            {
                try
                {
                    _logger.LogInformation("Successfully started story with title {Title}", story.Title);
                    return GetStoryPart(assistantContent);
                }
                catch (Exception e)
                {
                    _logger.LogError("Failed to deserialize the assistant content for story part: {Exception}", e);
                    if (attempt >= MAX_RETRY_ATTEMPTS - 1)
                    {
                        _logger.LogError("Reached maximum retry attempts for trying to deserialize the assistant content for story part");
                        throw new AIException("Failed to deserialize the assistant content for story part", e);
                    }
                    attempt++;
                }
            }

            throw new AIException("Failed to start story after maximum retry attempts");
        }

        // Benjamin Edlinger
        /// <summary>
        /// Generates the next part of the story based on the given story object
        /// </summary>
        /// <param name="story">The story object to generate the next part for</param>
        /// <returns>The next part of the story</returns>
        /// <exception cref="ArgumentException">If the story object is invalid</exception>
        /// <exception cref="AIException">If the next part could not be generated due to an error while fetching the content or deserializing</exception>
        public async Task<StoryPart> GenerateNextPart(Story story)
        {
            ArgumentNullException.ThrowIfNull(story);

            _logger.LogInformation("Generating next part of story with title {Title}", story.Title);
            List<ChatMessage> chatMessages;
            try
            {
                chatMessages = RebuildChatMessagesStory(story);
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to rebuild chat messages because of error in story object: {Exception}", e);
                throw new ArgumentException("Failed to rebuild chat messages because of error in story object", e);
            }

            string assistantContent = null!;
            int attempt = 0;
            while (attempt < MAX_RETRY_ATTEMPTS)
            {
                try
                {
                    assistantContent = await FetchAssitantContent(chatMessages, story.Temperature, story.TopP);
                    break;
                }
                catch (Exception e)
                {
                    _logger.LogError("Failed to fetch the assistant content for story part: {Exception}", e);
                    if (attempt >= MAX_RETRY_ATTEMPTS - 1)
                    {
                        _logger.LogError("Reached maximum retry attempts for trying to fetch the assistant content for story part");
                        throw new AIException("Failed to fetch the assistant content for story part", e);
                    }
                    attempt++;
                }
            }

            attempt = 0;
            while (attempt < MAX_RETRY_ATTEMPTS)
            {
                try
                {
                    _logger.LogInformation("Successfully generated next part of story with title {Title}", story.Title);
                    return GetStoryPart(assistantContent).Item1;
                }
                catch (Exception e)
                {
                    _logger.LogError("Failed to deserialize the assistant content for story part: {Exception}", e);
                    if (attempt >= MAX_RETRY_ATTEMPTS - 1)
                    {
                        _logger.LogError("Reached maximum retry attempts for trying to deserialize the assistant content for story part");
                        throw new AIException("Failed to deserialize the assistant content for story part", e);
                    }
                    attempt++;
                }
            }

            throw new AIException("Failed to generate next part after maximum retry attempts");
        }

        // Benjamin Edlinger
        /// <summary>
        /// Generates the result of the story based on the given story object
        /// </summary>
        /// <param name="story">The story object to generate the result for</param>
        /// <returns>The result of the story</returns>
        /// <exception cref="ArgumentException">If the story object is invalid</exception>
        /// <exception cref="AIException">If the result could not be generated due to an error while fetching the content or deserializing</exception>
        public async Task<StoryResult> GenerateResult(Story story)
        {
            ArgumentNullException.ThrowIfNull(story);

            _logger.LogInformation("Generating result of story with title {Title}", story.Title);
            List<ChatMessage> chatMessages;
            try
            {
                chatMessages = RebuildChatMessagesStory(story);
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to rebuild chat messages because of error in story object: {Exception}", e);
                throw new ArgumentException("Failed to rebuild chat messages because of error in story object", e);
            }

            string assistantContent = null!;
            int attempt = 0;
            while (attempt < MAX_RETRY_ATTEMPTS)
            {
                try
                {
                    assistantContent = await FetchAssitantContent(chatMessages, story.Temperature, story.TopP);
                    break;
                }
                catch (Exception e)
                {
                    _logger.LogError("Failed to fetch the assistant content for story part: {Exception}", e);
                    if (attempt >= MAX_RETRY_ATTEMPTS - 1)
                    {
                        _logger.LogError("Reached maximum retry attempts for trying to fetch the assistant content for story part");
                        throw new AIException("Failed to fetch the assistant content for story part", e);
                    }
                    attempt++;
                }
            }

            StoryPart endPart = null!;
            attempt = 0;
            while (attempt < MAX_RETRY_ATTEMPTS)
            {
                try
                {
                    endPart = GetStoryPart(assistantContent).Item1;
                    break;
                }
                catch (Exception e)
                {
                    _logger.LogError("Failed to deserialize the assistant content for story part: {Exception}", e);
                    if (attempt >= MAX_RETRY_ATTEMPTS - 1)
                    {
                        _logger.LogError("Reached maximum retry attempts for trying to deserialize the assistant content for story part");
                        throw new AIException("Failed to deserialize the assistant content for story part", e);
                    }
                    attempt++;
                }
            }

            string end = endPart.Text;
            try
            {
                chatMessages = RebuildChatMessagesResult(story, chatMessages, end);
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to rebuild chat messages for result because of error in story object: {Exception}", e);
                throw new ArgumentException("Failed to rebuild chat messages for result because of error in story object", e);
            }

            attempt = 0;
            while (attempt < MAX_RETRY_ATTEMPTS)
            {
                try
                {
                    assistantContent = await FetchAssitantContent(chatMessages, story.Temperature, story.TopP);
                    break;
                }
                catch (Exception e)
                {
                    _logger.LogError("Failed to fetch the assistant content for result: {Exception}", e);
                    if (attempt >= MAX_RETRY_ATTEMPTS - 1)
                    {
                        _logger.LogError("Reached maximum retry attempts for trying to fetch the assistant content for result");
                        throw new AIException("Failed to fetch the assistant content for result", e);
                    }
                    attempt++;
                }
            }

            attempt = 0;
            while (attempt < MAX_RETRY_ATTEMPTS)
            {
                try
                {
                    _logger.LogInformation("Successfully generated result of story with title {Title}", story.Title);
                    return GetStoryResult(assistantContent, end);
                }
                catch (Exception e)
                {
                    _logger.LogError("Failed to deserialize the assistant content for result: {Exception}", e);
                    if (attempt >= MAX_RETRY_ATTEMPTS - 1)
                    {
                        _logger.LogError("Reached maximum retry attempts for trying to deserialize the assistant content for result");
                        throw new AIException("Failed to deserialize the assistant content for result", e);
                    }
                    attempt++;
                }
            }

            throw new AIException("Failed to generate result after maximum retry attempts");
        }

/// <summary>
        /// Kacper Bohaczyk ----------------------------------------------------------------------------------------------------------------------
        /// </summary>
        /// <param name="story"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>

        private static List<ChatMessage> RebuildChatMessagesQuiz(Story story, QuizRequest config, List<ChatMessage> chatMessages)
        {



            ArgumentNullException.ThrowIfNull(story);

            string targetGroupString = story.TargetGroup switch
            {
                TargetGroup.PrimarySchool => "Die Teilnehmer, welche den Quiz  durchführen, sind Volksschüler im Alter von 6 bis 10 Jahren. Pass deinen Stil an diese Zielgruppe an und verwende einfache Sprache mit kurzen und klaren Sätzen.",
                TargetGroup.MiddleSchool => "Die Teilnehmer, welche den Quiz  durchführen, sind Schüler der Sekundarstufe eins im Alter von 11 bis 14 Jahren. Pass deinen Stil an diese Zielgruppe und verwende einen passend anspruchsvollen Wortschatz und Satzbau.",
                TargetGroup.HighSchool => "Die Teilnehmer, welche den Quiz  durchführen, sind Schüler der Sekundarstufe zwei im Alter von 15 bis 19 Jahren. Pass deinen Stil an diese Zielgruppe an und verwende eine anspruchsvollere Sprache mit komplexeren Satzstrukturen und Fachbegriffen.",
                _ => throw new ArgumentException("Invalid target group")
            };
            // Der Teil drüber soll zusätylich in den SystemPromt reinkommen

            string systemPrompt = "Von jetzt an versetzt du dich in die Rolle eines proffesionellem Quizersteller mit besonderer Expertise in dem Bereich Nachhaltigkeit." +
                                   "Deine Aufgabe ist, ein Quiz zu erstellen, welcher auf der zuvor generierten Geschichte basiert." +
                                   "Die Fragen sollen sich ausschließlich auf die in der Geschichte thematisierten Nachhaltigkeitsaspekte konzentrieren, und im genauerem dem ausgewählten Pfad vom User folgen. " +
                                   "Wichtig ist es das du den ganzen Quiz, das beduetet die Fragen und die Antorten in  einer Response ausgibst. " +
                                   $"Formatierungsrichtlinien: {targetGroupString} " +
                                  "{'Title': 'Der Titel des ganzen Quizes', 'NumberQuestions': 'Anzahl an Questions', 'Questions': {'Text': 'Der Titel der jeweiligen Frage','Text': 'Der Titel der jeweiligen Frage', 'Number': 'Die Nummer der Frage', 'Choices': [{'Number': 'Die Nummer der Auswahlmöglichkeit', 'Text': 'Der Text zur jeweiligen Auswahlmöglichkeit', 'IsCorrect': 'Ein Wahrheitswert, der angibt, ob die Auswahl korrekt ist'}]}} " +
                                   $"Das Quiz soll aus" +
                                   string.Join(", ", config.Types.Select(t =>
                                   {
                                       return t switch
                                       {
                                           QuizType.MultipleResponse => "Multiple response-",
                                           QuizType.SingleResponse => "Single response-",
                                           QuizType.TrueFalse => "True/False-",
                                           _ => throw new ArgumentException("Invalid quiz type")
                                       };
                                   })) +
                                   $"Fragen bestehen und soll {config.NumberQuestions} Fragen lang sein";
            ;
            string userPrompt = "Generiere das Quiz auf Basis der durchlebten Story.";

            chatMessages.Add(new ChatMessage { Role = ValidRoles.System, Content = systemPrompt });
            chatMessages.Add(new ChatMessage { Role = ValidRoles.User, Content = userPrompt });

            return chatMessages;
        }



/// <summary>
/// Kacper Bohaczyk
/// </summary>
/// <param name="userName"></param> --------------------------------------------------------------------------------------------------------
/// <returns></returns>
    public async Task<string> GenerateProfileImage(String userName, ImageStyle style){
            ArgumentNullException.ThrowIfNull(_client);
            ArgumentNullException.ThrowIfNull(userName);


            String imagePrompt = $"Generate an image related to sustainability that matches the term '{userName}'. The Style of the image should match the {style} style";


            HttpRequestMessage requestImage = new(HttpMethod.Post, "/v1/inference/black-forest-labs/FLUX-1-dev")
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    prompt = imagePrompt,
                    width = 128,
                    height = 128
                }), Encoding.UTF8, "application/json")
            };
            HttpResponseMessage responseImage = null!;
            string responseStringImage;

            try
            {
                responseImage = await _client.SendAsync(requestImage);
                responseImage.EnsureSuccessStatusCode();
                responseStringImage = await responseImage.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                throw new AIException($"Request for image generation failed with status code {responseImage?.StatusCode}", e);
            }
            string base64String;
            try
            {
                ImageContent responseObjectImage = JsonSerializer.Deserialize<ImageContent>(responseStringImage) ?? throw new InvalidOperationException("Response object is null");
                base64String = responseObjectImage.Images[0];
                if (base64String.StartsWith("data:image/png;base64,"))
                {
                    base64String = base64String.Replace("data:image/png;base64,", string.Empty);
                }
                else
                {
                    throw new InvalidOperationException("Image content is not a base64 string");
                }
            }
            catch (JsonException e)
            {
                throw new AIException("Failed to deserialize response content", e);
            }
            string folderName, fileName;
            try
            {
                var wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                folderName = "images";
                var directoryPath = Path.Combine(wwwRootPath, folderName);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                fileName = $"{Guid.NewGuid()}.png";
                var filePath = Path.Combine(directoryPath, fileName);
                byte[] imageBytes = Convert.FromBase64String(base64String);
                using (var ms = new MemoryStream(imageBytes))
                {
                    using var skImage = SKImage.FromEncodedData(ms);
                    using var skData = skImage.Encode(SKEncodedImageFormat.Png, 100);
                    using var fileStream = File.OpenWrite(filePath);
                    skData.SaveTo(fileStream);
                }
            }
            catch (Exception e)
            {
                throw new AIException("Failed to save image", e);
            }

            return Path.Combine("/", folderName, fileName).Replace("\\", "/");


    }

        // --------------------------------------------------------------------------------------------------------------------------------------------
        // Benjamin Edlinger
        /// <summary>
        /// Rebuilds the chat messages of the story for the given story object
        /// </summary>
        /// <param name="story">The story object to rebuild the chat messages for</param>
        /// <returns>The rebuilt chat messages</returns>
        /// <exception cref="ArgumentException">If the story object is invalid</exception>
        /// <exception cref="ArgumentNullException">If the story object is null</exception>
        private static List<ChatMessage> RebuildChatMessagesStory(Story story)
        {
            ArgumentNullException.ThrowIfNull(story);

            string targetGroupString = story.TargetGroup switch
            {
                TargetGroup.PrimarySchool => "Die Geschichte wird für Volksschüler (6-10 Jahre) erstellt. Passe Sprachstil, Wortwahl und Inhalte genau an die jeweilige Altersgruppe an:"
                    + "- Volksschüler: Verwende einfache Sprache, kurze Sätze und erkläre schwierige Begriffe mit Alltagsbeispielen.",
                TargetGroup.MiddleSchool => "Die Geschichte wird für Schüler der Sekundarstufe eins (10-14 Jahre) erstellt. Passe Sprachstil, Wortwahl und Inhalte genau an die jeweilige Altersgruppe an:"
                    + "- Sekundarstufe eins: Nutze lebendige, verständliche Sprache und integriere moralische Konflikte, die greifbar sind.",
                TargetGroup.HighSchool => "Die Geschichte wird für Schüler der Sekundarstufe zwei (15-19 Jahre) erstellt. Passe Sprachstil, Wortwahl und Inhalte genau an die jeweilige Altersgruppe an:"
                    + "- Sekundarstufe zwei: Verwende komplexere Satzstrukturen, Fachbegriffe und beleuchte globale Zusammenhänge der Nachhaltigkeit.",
                _ => throw new ArgumentException("Invalid target group")
            };
            string lengthRequirement = story.TargetGroup switch
            {
                TargetGroup.PrimarySchool => "Jeder Abschnitt soll mindestens 60 Wörter umfassen, in einfachen Sätzen und mit kurzen Absätzen.",
                TargetGroup.MiddleSchool => "Jeder Abschnitt soll mindestens 125 Wörter umfassen, mit verständlicher Sprache und anschaulichen Beispielen.",
                TargetGroup.HighSchool => "Jeder Abschnitt soll mindestens 160 Wörter umfassen, mit detaillierten Beschreibungen, komplexen Satzstrukturen und umfangreichen Erklärungen.",
                _ => throw new ArgumentException("Invalid target group")
            };
            string systemPrompt = "Du bist ein Geschichtenerzähler, der interaktive und textbasierte Geschichten zum Thema Nachhaltigkeit erstellt. Bitte beachte folgende Vorgaben:"
                + "[Thema]"
                + story.Topic
                + "[Zielgruppe]"
                + targetGroupString
                + "[Interaktivität]"
                + "Die Geschichte ist in mehrere Abschnitte unterteilt und enthält an jedem Entscheidungspunkt vier Optionen. Beachte:"
                + "- Jede Option hat einen Einflusswert zwischen -1 (starker negativer Einfluss) und 1 (starker positiver Einfluss)."
                + "- Die Summe der Einflusswerte der vier Optionen muss immer 0 ergeben."
                + "- Bei jedem Entscheidungspunkt präsentiere die vier Optionen und warte auf die Wahl der Teilnehmer."
                + "- Bei Erreichen des letzten Entscheidungspunkts, setze den Abschluss der Geschichte um."
                + "[Länge und Detailtiefe]"
                + lengthRequirement
                + "[Formatierung]"
                + "Antworte ausschließlich im folgenden JSON-Format:"
                + "{"
                + "  \"title\": \"Titel der Geschichte\","
                + "  \"intertitle\": \"Zwischentitel des Abschnitts\","
                + "  \"story\": \"Text des aktuellen Abschnitts.\","
                + "  \"options\": ["
                + "    { \"impact\": \"Wert zwischen -1 und 1\", \"text\": \"Beschreibung der Option 1\" },"
                + "    { \"impact\": \"Wert zwischen -1 und 1\", \"text\": \"Beschreibung der Option 2\" },"
                + "    { \"impact\": \"Wert zwischen -1 und 1\", \"text\": \"Beschreibung der Option 3\" },"
                + "    { \"impact\": \"Wert zwischen -1 und 1\", \"text\": \"Beschreibung der Option 4\" }"
                + "  ]"
                + "}"
                + "Stelle sicher, dass das JSON fehlerfrei geparst werden kann."
                + "[Kontext und Fortführung]"
                + "Berücksichtige alle bisherigen Entscheidungen und ihre Konsequenzen. Jeder Abschnitt sollte nahtlos an den vorherigen anschließen und die Geschichte weiterentwickeln.";
            string userPrompt = "Alle Teilnehmer sind bereit. Beginne bitte mit dem ersten Abschnitt deiner Geschichte zum Thema Nachhaltigkeit. "
                + $"Die Geschichte umfasst insgesamt {story.Length} Entscheidungspunkte."
                + "Bitte beachte:"
                + "- Verwende den vorgegebenen Sprachstil für die jeweilige Zielgruppe."
                + "- Erstelle den ersten Abschnitt inklusive eines Entscheidungspunkts, bei dem vier Optionen (mit jeweiligen Einflusswerten zwischen -1 und 1, deren Summe 0 ergeben muss) eingebaut werden."
                + "- Die Antwort muss exakt im folgenden JSON-Format erfolgen:"
                + "{"
                + "  \"title\": \"Titel der Geschichte\","
                + "  \"intertitle\": \"Zwischentitel des Abschnitts\","
                + "  \"story\": \"Text des aktuellen Abschnitts.\","
                + "  \"options\": ["
                + "    { \"impact\": \"Wert zwischen -1 und 1\", \"text\": \"Beschreibung der Option 1\" },"
                + "    { \"impact\": \"Wert zwischen -1 und 1\", \"text\": \"Beschreibung der Option 2\" },"
                + "    { \"impact\": \"Wert zwischen -1 und 1\", \"text\": \"Beschreibung der Option 3\" },"
                + "    { \"impact\": \"Wert zwischen -1 und 1\", \"text\": \"Beschreibung der Option 4\" }"
                + "  ]"
                + "}"
                + "Bitte beginne jetzt mit dem ersten Abschnitt.";

            List<ChatMessage> chatMessages =
            [
                new() { Role = ValidRoles.System, Content = systemPrompt },
                new() { Role = ValidRoles.User, Content = userPrompt }
            ];

            foreach (var part in story.Parts.Select((value, index) => new { value, index }))
            {
                StoryContent assitentContent = new()
                {
                    Title = story.Title ?? throw new ArgumentNullException("Story has no title"),
                    Intertitle = part.value.Intertitle,
                    Story = part.value.Text,
                    Options = part.value.Choices.Select(choice => new Option
                    {
                        ImpactString = choice.Impact.ToString(CultureInfo.InvariantCulture),
                        Text = choice.Text
                    }).ToList()
                };
                chatMessages.Add(new ChatMessage { Role = ValidRoles.Assistant, Content = JsonSerializer.Serialize(assitentContent) });

                if (!part.value.ChosenNumber.HasValue || part.value.ChosenNumber < 1 || part.value.ChosenNumber > 4)
                {
                    throw new ArgumentException($"Story part with id {part.value.Id} has invalid choice number");
                }
                else if (story.Length == part.index + 1)
                {
                    userPrompt = $"Die Option {part.value.ChosenNumber} \"{part.value.Text}\" wurde gewählt. Du hast nun den letzten Entscheidungspunkt erreicht. Bitte schreibe den abschließenden Teil der Geschichte."
                        + "Achte darauf:"
                        + "- Führe die Geschichte konsequent zu einem runden Abschluss, indem du alle vorherigen Ereignisse berücksichtigst."
                        + "- Im letzten Abschnitt soll es keinen weiteren Entscheidungspunkt mehr geben. Daher müssen die Optionen im JSON-Array als leere, aber valide Einträge erscheinen (z. B. leere Strings)."
                        + "- Die Antwort muss weiterhin exakt im folgenden JSON-Format erfolgen:"
                        + "{"
                        + "  \"title\": \"Titel der Geschichte\","
                        + "  \"intertitle\": \"Zwischentitel des Schlussabschnitts\","
                        + "  \"story\": \"Abschließender Text der Geschichte, der alle Handlungsstränge zusammenführt.\","
                        + "  \"options\": ["
                        + "    { \"impact\": \"\", \"text\": \"\" },"
                        + "    { \"impact\": \"\", \"text\": \"\" },"
                        + "    { \"impact\": \"\", \"text\": \"\" },"
                        + "    { \"impact\": \"\", \"text\": \"\" }"
                        + "  ]"
                        + "}"
                        + "Bitte beende jetzt die Geschichte.";
                    chatMessages.Add(new ChatMessage { Role = ValidRoles.User, Content = userPrompt });
                }
                else
                {
                    userPrompt = $"Die Option {part.value.ChosenNumber} \"{part.value.Text}\"  wurde gewählt. Bitte fahre mit dem nächsten Abschnitt der Geschichte fort. Achte darauf:"
                        + "- Den bisherigen Kontext und die Konsequenzen der getroffenen Entscheidungen nahtlos einzubauen."
                        + "- Einen neuen Entscheidungspunkt zu integrieren, der wieder vier Optionen enthält (mit den Einflusswerten, deren Summe exakt 0 beträgt)."
                        + "- Den neuen Abschnitt im vorgegebenen JSON-Format auszugeben:"
                        + "{"
                        + "  \"title\": \"Titel der Geschichte\","
                        + "  \"intertitle\": \"Zwischentitel des neuen Abschnitts\","
                        + "  \"story\": \"Text des aktuellen Abschnitts, der auf den bisherigen Ereignissen aufbaut.\","
                        + "  \"options\": ["
                        + "    { \"impact\": \"Wert zwischen -1 und 1\", \"text\": \"Beschreibung der Option 1\" },"
                        + "    { \"impact\": \"Wert zwischen -1 und 1\", \"text\": \"Beschreibung der Option 2\" },"
                        + "    { \"impact\": \"Wert zwischen -1 und 1\", \"text\": \"Beschreibung der Option 3\" },"
                        + "    { \"impact\": \"Wert zwischen -1 und 1\", \"text\": \"Beschreibung der Option 4\" }"
                        + "  ]"
                        + "}"
                        + "Bitte setze die Geschichte an dieser Stelle fort.";
                    chatMessages.Add(new ChatMessage { Role = ValidRoles.User, Content = userPrompt });
                }
            }

            return chatMessages;
        }

        // Benjamin Edlinger
        /// <summary>
        /// Rebuilds the chat messages of the result for already rebuilt chat messages and the given story object
        /// </summary>
        /// <param name="story">The story object to rebuild the chat messages for</param>
        /// <param name="chatMessages">The already rebuilt chat messages based on the story</param>
        /// <param name="end">The end of the story</param>
        /// <returns>The rebuilt chat messages</returns>
        /// <exception cref="ArgumentNullException">If the story object is null, the chat messages are null or the end is null</exception>
        /// <exception cref="ArgumentException">If the chat messages list is empty</exception>
        private static List<ChatMessage> RebuildChatMessagesResult(Story story, List<ChatMessage> chatMessages, string end)
        {
            ArgumentNullException.ThrowIfNull(story);
            ArgumentNullException.ThrowIfNull(chatMessages);
            ArgumentNullException.ThrowIfNull(end);
            if (chatMessages.Count == 0) throw new ArgumentException("No messages to send");

            chatMessages.Add(new ChatMessage { Role = ValidRoles.Assistant, Content = end });
            string targetGroupString = story.TargetGroup switch
            {
                TargetGroup.PrimarySchool => "Für Volksschüler (6-10 Jahre): Verwende einfache, bildhafte Sprache, kurze Sätze und anschauliche Beispiele, die aus dem Alltag der Kinder stammen.",
                TargetGroup.MiddleSchool => "- Für Schüler der Sekundarstufe eins (11-14 Jahre): Nutze einen lebendigen, verständlichen Sprachstil und integriere altersgerechte Erklärungen und Beispiele. Baue moralische Konflikte ein, die für diesen Altersbereich nachvollziehbar sind.",
                TargetGroup.HighSchool => "- Für Schüler der Sekundarstufe zwei (15-19 Jahre): Verwende einen anspruchsvolleren Sprachstil mit komplexeren Satzstrukturen und gegebenenfalls Fachbegriffen, um tiefere Zusammenhänge und globale Perspektiven zu beleuchten.",
                _ => throw new ArgumentException("Invalid target group")
            };
            string systemPrompt = "Du übernimmst die Rolle einer Lehrkraft, die gemeinsam mit den Teilnehmern die gerade durchlebte Geschichte zum Thema Nachhaltigkeit reflektiert."
                + "Bitte beachte dabei, dass du deinen Sprachstil, die Beispiele und die Diskussionsfragen an die jeweilige Zielgruppe anpasst:"
                + targetGroupString
                + "Deine Aufgabe besteht darin, den thematischen Kontext und die getroffenen Entscheidungen faktenbasiert und verständlich zu analysieren. Bitte folge diesen Schritten:"
                + "[Zusammenfassung und Analyse]"
                + "- Fasse die Geschichte in einem kurzen, prägnanten Fließtext zusammen. Stelle den Verlauf und die zentralen Ereignisse übersichtlich dar."
                + "- Analysiere, wie die Entscheidungen und Handlungen der Charaktere den Verlauf der Geschichte beeinflusst haben."
                + "[Positive und negative Entscheidungen]"
                + "- Erstelle eine Liste der positiven Entscheidungen, die in der Geschichte getroffen wurden. Erkläre zu jeder Entscheidung, warum sie sich positiv ausgewirkt hat und welche konkreten Vorteile daraus entstanden sind."
                + "- Erstelle eine Liste der negativen Entscheidungen. Beschreibe jeweils, welche negativen Konsequenzen daraus resultierten und wie sie den Verlauf der Geschichte beeinflusst haben."
                + "[Praktische Lehren]"
                + "- Ziehe konkrete Lehren aus der Geschichte und übertrage diese Erkenntnisse auf die reale Welt. Zeige auf, wie diese praktischen Erkenntnisse im Alltag oder in spezifischen Situationen angewendet werden können."
                + "[Diskussionsfragen]"
                + "- Formuliere gezielte Fragen, die zu einer offenen und respektvollen Diskussion anregen. Passe die Komplexität der Fragen an die Zielgruppe an, sodass sie zum Nachdenken anregen und unterschiedliche Perspektiven einbeziehen."
                + "Wichtig: Deine Analyse muss den nachhaltigen Kontext der Geschichte widerspiegeln und gleichzeitig sprachlich sowie inhaltlich auf die Zielgruppe abgestimmt sein."
                + "Bitte antworte ausschließlich im gültigen JSON-Format, damit deine Antwort korrekt dargestellt wird."
                + "Das erwartete JSON-Format lautet:"
                + "{"
                + "  \"summary\": \"Zusammenfassung und Analyse der Geschichte als Fließtext\","
                + "  \"positive_choices\": [\"Beschreibung der positiven Entscheidung 1\", \"Weitere positive Entscheidungen je nach Bedarf\"],"
                + "  \"negative_choices\": [\"Beschreibung der negativen Entscheidung 1\", \"Weitere negative Entscheidungen je nach Bedarf\"],"
                + "  \"learnings\": [\"Erkenntnis 1\", \"Weitere Erkenntnisse je nach Bedarf\"],"
                + "  \"discussion_questions\": [\"Frage 1\", \"Weitere Fragen je nach Bedarf\"]"
                + "}";
            chatMessages.Add(new ChatMessage { Role = ValidRoles.System, Content = systemPrompt });
            string userPrompt = "Die Geschichte ist soeben beendet. Du kannst nun die Analyse der durchlebten Geschichte erstellen. Denke daran, deinen Sprachstil, deine Beispiele und Diskussionsfragen an die Zielgruppe anzupassen. Bitte folge dabei genau den genannten Anweisungen und dem vorgegebenen JSON-Format.";
            chatMessages.Add(new ChatMessage { Role = ValidRoles.User, Content = userPrompt });

            if (story.Result != null)
            {
                AnalysisContent assistentContent = new()
                {
                    Summary = story.Result.Summary,
                    PositiveChoices = story.Result.PositiveChoices,
                    NegativeChoices = story.Result.NegativeChoices,
                    Learnings = story.Result.Learnings,
                    DiscussionQuestions = story.Result.DiscussionQuestions
                };
                chatMessages.Add(new ChatMessage { Role = ValidRoles.Assistant, Content = JsonSerializer.Serialize(assistentContent) });
            }

            return chatMessages;
        }

        // Benjamin Edlinger
        /// <summary>
        /// Gets the story part from the given assistant content
        /// </summary>
        /// <param name="assistantContent">The assistant content to get the story part from</param>
        /// <returns>The story part and the title of the story</returns>
        /// <exception cref="InvalidOperationException">If the message content is null</exception>
        /// <exception cref="JsonException">If the assistant content could not be deserialized</exception>
        private static (StoryPart, string) GetStoryPart(string assistantContent)
        {
            ArgumentNullException.ThrowIfNull(assistantContent);

            StoryContent messageContent;
            try
            {
                messageContent = JsonSerializer.Deserialize<StoryContent>(assistantContent) ?? throw new InvalidOperationException("Message content is null");
            }
            catch (JsonException e)
            {
                throw new JsonException("Failed to deserialize assistant content", e);
            }

            StoryPart storyPart = new()
            {
                Text = messageContent.Story,
                Intertitle = messageContent.Intertitle,
                Choices = messageContent.Options.Select((option, index) => new StoryChoice
                {
                    Text = option.Text,
                    Number = index + 1,
                    Impact = option.Impact
                }).ToList()
            };
            return (storyPart, messageContent.Title);
        }

        // Benjamin Edlinger
        /// <summary>
        /// Gets the story result from the given assistant content
        /// </summary>
        /// <param name="assistantContent">The assistant content to get the story result from</param>
        /// <param name="end">The end of the story</param>
        /// <returns>The story result</returns>
        /// <exception cref="InvalidOperationException">If the message content is null</exception>
        /// <exception cref="JsonException">If the assistant content could not be deserialized</exception>
        private static StoryResult GetStoryResult(string assistantContent, string end)
        {
            ArgumentNullException.ThrowIfNull(assistantContent);
            ArgumentNullException.ThrowIfNull(end);

            AnalysisContent messageContent;
            try
            {
                messageContent = JsonSerializer.Deserialize<AnalysisContent>(assistantContent) ?? throw new InvalidOperationException("Message content is null");
            }
            catch (JsonException e)
            {
                throw new JsonException("Failed to deserialize assistant content", e);
            }

            return new StoryResult
            {
                Text = end,
                Summary = messageContent.Summary,
                PositiveChoices = messageContent.PositiveChoices,
                NegativeChoices = messageContent.NegativeChoices,
                Learnings = messageContent.Learnings,
                DiscussionQuestions = messageContent.DiscussionQuestions
            };
        }

        // Benjamin Edlinger
        /// <summary>
        /// Fetches the assistant content based on the given chat messages, temperature and topP
        /// </summary>
        /// <param name="chatMessages">The chat messages to fetch the assistant content for</param>
        /// <param name="temperature">The temperature for the assistant content</param>
        /// <param name="topP">The topP for the assistant content</param>
        /// <param name="isJsonResponse">If the response should be a json object</param>
        /// <returns>The assistant content</returns>
        /// <exception cref="ArgumentException">If the chat messages are empty, the temperature is invalid or the topP is invalid</exception>
        /// <exception cref="HttpRequestException">If the request failed</exception>
        /// <exception cref="InvalidOperationException">If the response object is null or the assistant content is null or empty</exception>
        /// <exception cref="JsonException">If the response content could not be deserialized</exception>
        private async Task<string> FetchAssitantContent(List<ChatMessage> chatMessages, float temperature, float topP, bool isJsonResponse = true)
        {
            ArgumentNullException.ThrowIfNull(_client);
            ArgumentNullException.ThrowIfNull(chatMessages);
            if (chatMessages.Count == 0) throw new ArgumentException("No messages to send");
            if (temperature < 0 || temperature > 1) throw new ArgumentException("Invalid temperature");
            if (topP < 0 || topP > 1) throw new ArgumentException("Invalid topP");

            object requestBody;
            if (isJsonResponse)
            {
                requestBody = new
                {
                    model = "meta-llama/Llama-3.3-70B-Instruct",
                    messages = chatMessages,
                    response_format = new { type = "json_object" },
                    temperature,
                    top_p = topP
                };
            }
            else
            {
                requestBody = new
                {
                    model = "meta-llama/Llama-3.3-70B-Instruct",
                    messages = chatMessages,
                    temperature,
                    top_p = topP
                };
            }
            HttpRequestMessage request = new(HttpMethod.Post, "/v1/openai/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            HttpResponseMessage response = null!;
            string responseString;
            try
            {
                response = await _client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                responseString = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                throw new HttpRequestException($"Request failed with status code {response?.StatusCode}", e);
            }

            try
            {
                Response responseObject = JsonSerializer.Deserialize<Response>(responseString) ?? throw new InvalidOperationException("Response object is null");
                _logger.LogDebug("Assistant response: {Response}", responseObject.Choices[0].Message.Content);
                return responseObject.Choices[0].Message.Content ?? throw new InvalidOperationException("Assistant content is null or empty");
            }
            catch (JsonException e)
            {
                throw new JsonException("Failed to deserialize response content", e);
            }
        }

        // Benjamin Edlinger ---------------------------------------------Darauf kann man schauen------------------------------------------------------------
        /// <summary>
        /// Generates an image based on the given story object
        /// </summary>
        /// <param name="story">Based on this story object the image will be generated</param>
        /// <returns>The path to the generated image</returns>
        /// <exception cref="AIException">If the request for generating the prompt failed, the request for generating the image failed, the response content could not be deserialized or the image could not be saved</exception>
        /// <exception cref="InvalidOperationException">If the response object is null, the assistant content is null or empty, the image content is not a base64 string or the image content is null</exception>
        public async Task<string> GenerateStoryImage(Story story)
        {
            ArgumentNullException.ThrowIfNull(_client);
            ArgumentNullException.ThrowIfNull(story);

            string systemPrompt = "Du bist ein professioneller Prompt Engineer, der sich auf die Erstellung hochdetaillierter und konsistenter Bildbeschreibungen für KI-basierte Bildgenerierungssysteme spezialisiert hat. Deine Aufgabe ist es, Prompts zu entwerfen, die zu visuell harmonischen und stilistisch kohärenten Bildern führen. Achte darauf, dass die Beschreibung lebendig und spezifisch ist und alle relevanten Details umfasst – dazu gehören:"
                + "- Setting: Ort, Umgebung und atmosphärische Details"
                + "- Charaktere: Aussehen, Mimik, Kleidung und Ausdruck"
                + "- Beleuchtung und Farben: Lichtverhältnisse, Farbschema, Stimmung"
                + "- Stimmung und Atmosphäre: Emotionale Wirkung und erzählerischer Ton"
                + "- Künstlerischer Stil: Angabe von Technik, Epoche oder Medium (z.B. „ein surrealistisches Ölgemälde des 19. Jahrhunderts“ oder „eine cinematische, photorealistische Szene mit weichem Licht“)"
                + "Wichtig: Alle Elemente müssen im Einklang mit der Erzählung der Geschichte stehen, ohne widersprüchliche oder unpassende Stilmittel."
                + "Zusätzlich ist der künstlerische Stil an die Zielgruppe anzupassen:"
                + "- Volksschüler (6-10 Jahre): Verwende einen cartoonhaften, verspielten Stil mit kräftigen Primärfarben, einfachen Formen und freundlichen, fantasievollen Charakteren."
                + "- Schüler der Sekundarstufe eins (11-14 Jahre): Wähle einen halb-realistischen oder stilisierten Stil mit lebendigen, klaren Farben und dynamischen Kompositionen, der Abenteuer und leichte Dramatik vermittelt."
                + "- Schüler der Sekundarstufe zwei (15-19 Jahre): Setze auf einen detaillierten, photorealistischen Stil mit anspruchsvoller Beleuchtung, realistischen Texturen und einer ernsten, nachdenklichen Atmosphäre."
                + "Dein Ziel ist es, prägnante, kreative und präzise Prompts zu erstellen, die die KI optimal anleiten, beeindruckende Bilder zu generieren.";
            string text = story.Result != null ? story.Result.Text : story.Parts.Last().Text;
            ArgumentException.ThrowIfNullOrEmpty(text);
            string targetStyle = story.TargetGroup switch
            {
                TargetGroup.PrimarySchool => "- Für Volksschüler: cartoonhaft, verspielt und mit kräftigen Primärfarben.",
                TargetGroup.MiddleSchool => "- Für Sekundarstufe eins: halb-realistischer/stilisierter Stil, lebendig und dynamisch.",
                TargetGroup.HighSchool => "- Für Sekundarstufe zwei: detailliert, photorealistisch und ernst.",
                _ => throw new ArgumentException("Invalid target group")
            };
            string userPrompt = $"Erstelle einen detaillierten und lebendigen Prompt für ein KI-basiertes Bildgenerierungssystem basierend auf folgendem Storypart: \"{text}\"."
                + "Bitte stelle sicher, dass:"
                + "- Alle relevanten Details wie Setting, Charaktere, Beleuchtung, Farben, Stimmung und künstlerischer Stil in der Bildbeschreibung enthalten sind."
                + "- Die Bildbeschreibung vollständig mit der Erzählung übereinstimmt und alle Elemente stilistisch harmonisch aufeinander abgestimmt sind."
                + "- Der künstlerische Stil passgenau an die Zielgruppe angepasst wird:"
                + targetStyle
                + "- Du eine klare, präzise und bildhafte Sprache verwendest, um die KI optimal anzuleiten."
                + "Nutze diese Anweisungen, um einen hochwertigen, zielgruppenspezifischen Bildprompt zu generieren."
                + "Antworte nur auf Englisch!";

            List<ChatMessage> chatMessages =
            [
                new ChatMessage { Role = ValidRoles.System, Content = systemPrompt },
                new ChatMessage { Role = ValidRoles.User, Content = userPrompt }
            ];

            string imagePrompt;
            try
            {
                imagePrompt = await FetchAssitantContent(chatMessages, story.Temperature, story.TopP, false);
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to fetch the image prompt: {Exception}", e);
                throw new AIException("Failed to fetch the image prompt", e);
            }

            HttpRequestMessage requestImage = new(HttpMethod.Post, "/v1/inference/black-forest-labs/FLUX-1-dev")
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    prompt = imagePrompt,
                    // approx. 2.67:1 ratio
                    width = 1000,
                    height = 250
                }), Encoding.UTF8, "application/json")
            };
            HttpResponseMessage responseImage = null!;
            string responseStringImage;
            try
            {
                responseImage = await _client.SendAsync(requestImage);
                responseImage.EnsureSuccessStatusCode();
                responseStringImage = await responseImage.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                _logger.LogError("Request for image generation failed with status code {StatusCode}", responseImage?.StatusCode);
                throw new AIException($"Request for image generation failed with status code {responseImage?.StatusCode}", e);
            }
            string base64String;
            try
            {
                ImageContent responseObjectImage = JsonSerializer.Deserialize<ImageContent>(responseStringImage) ?? throw new InvalidOperationException("Response object is null");
                base64String = responseObjectImage.Images[0];
                if (base64String.StartsWith("data:image/png;base64,"))
                {
                    base64String = base64String.Replace("data:image/png;base64,", string.Empty);
                }
                else
                {
                    _logger.LogError("Image content is not a base64 string");
                    throw new InvalidOperationException("Image content is not a base64 string");
                }
            }
            catch (JsonException e)
            {
                _logger.LogError("Failed to deserialize response content: {Exception}", e);
                throw new AIException("Failed to deserialize response content", e);
            }
            string folderName, fileName;
            try
            {
                var wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                folderName = "images";
                var directoryPath = Path.Combine(wwwRootPath, folderName);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                fileName = $"{Guid.NewGuid()}.png";
                var filePath = Path.Combine(directoryPath, fileName);
                byte[] imageBytes = Convert.FromBase64String(base64String);
                using (var ms = new MemoryStream(imageBytes))
                {
                    using var skImage = SKImage.FromEncodedData(ms);
                    using var skData = skImage.Encode(SKEncodedImageFormat.Png, 100);
                    using var fileStream = File.OpenWrite(filePath);
                    skData.SaveTo(fileStream);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to save image: {Exception}", e);
                throw new AIException("Failed to save image", e);
            }

            return Path.Combine("/", folderName, fileName).Replace("\\", "/");
        }

        /// <summary>
        /// Kacper Bohaczyk
        /// </summary>
        /// <param name="story"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        private static List<ChatMessage> RebuildChatMessagesQuiz(Story story, QuizRequest config, List<ChatMessage> chatMessages)
        {



            ArgumentNullException.ThrowIfNull(story);

            string targetGroupString = story.TargetGroup switch
            {
                TargetGroup.PrimarySchool => "Die Teilnehmer, welche den Quiz  durchführen, sind Volksschüler im Alter von 6 bis 10 Jahren. Pass deinen Stil an diese Zielgruppe an und verwende einfache Sprache mit kurzen und klaren Sätzen.",
                TargetGroup.MiddleSchool => "Die Teilnehmer, welche den Quiz  durchführen, sind Schüler der Sekundarstufe eins im Alter von 11 bis 14 Jahren. Pass deinen Stil an diese Zielgruppe und verwende einen passend anspruchsvollen Wortschatz und Satzbau.",
                TargetGroup.HighSchool => "Die Teilnehmer, welche den Quiz  durchführen, sind Schüler der Sekundarstufe zwei im Alter von 15 bis 19 Jahren. Pass deinen Stil an diese Zielgruppe an und verwende eine anspruchsvollere Sprache mit komplexeren Satzstrukturen und Fachbegriffen.",
                _ => throw new ArgumentException("Invalid target group")
            };
            // Der Teil drüber soll zusätylich in den SystemPromt reinkommen

            string systemPrompt = "Von jetzt an versetzt du dich in die Rolle eines proffesionellem Quizersteller mit besonderer Expertise in dem Bereich Nachhaltigkeit." +
                                   "Deine Aufgabe ist, ein Quiz zu erstellen, welcher auf der zuvor generierten Geschichte basiert." +
                                   "Die Fragen sollen sich ausschließlich auf die in der Geschichte thematisierten Nachhaltigkeitsaspekte konzentrieren, und im genauerem dem ausgewählten Pfad vom User folgen. " +
                                   "Wichtig ist es das du den ganzen Quiz, das beduetet die Fragen und die Antorten in  einer Response ausgibst. " +
                                   $"Formatierungsrichtlinien: {targetGroupString} " +
                                  "{'Title': 'Der Titel des ganzen Quizes', 'NumberQuestions': 'Anzahl an Questions', 'Questions': {'Text': 'Der Titel der jeweiligen Frage','Text': 'Der Titel der jeweiligen Frage', 'Number': 'Die Nummer der Frage', 'Choices': [{'Number': 'Die Nummer der Auswahlmöglichkeit', 'Text': 'Der Text zur jeweiligen Auswahlmöglichkeit', 'IsCorrect': 'Ein Wahrheitswert, der angibt, ob die Auswahl korrekt ist'}]}} " +
                                   $"Das Quiz soll aus" +
                                   string.Join(", ", config.Types.Select(t =>
                                   {
                                       return t switch
                                       {
                                           QuizType.MultipleResponse => "Multiple response-",
                                           QuizType.SingleResponse => "Single response-",
                                           QuizType.TrueFalse => "True/False-",
                                           _ => throw new ArgumentException("Invalid quiz type")
                                       };
                                   })) +
                                   $"Fragen bestehen und soll {config.NumberQuestions} Fragen lang sein";
            ;
            string userPrompt = "Generiere das Quiz auf Basis der durchlebten Story.";

            chatMessages.Add(new ChatMessage { Role = ValidRoles.System, Content = systemPrompt });
            chatMessages.Add(new ChatMessage { Role = ValidRoles.User, Content = userPrompt });

            return chatMessages;
        }

        /// <summary>
        /// Kacper Bohaczyk
        /// </summary>
        /// <param name="assistantContent"></param>
        /// <returns></returns>
        private static Quiz GetQuiz(string assistantContent)
        {
            ArgumentNullException.ThrowIfNull(assistantContent);

            QuizContent messageContent;
            try
            {
                messageContent = JsonSerializer.Deserialize<QuizContent>(assistantContent) ?? throw new InvalidOperationException("Message content is null");
            }
            catch (JsonException e)
            {
                throw new JsonException("Failed to deserialize assistant content", e);
            }


            return new Quiz
            {
                Title = messageContent.Title,
                NumberQuestions = messageContent.NumberQuestions,
                Questions = messageContent.Questions.Select((question, index) => new QuizQuestion
                {
                    Number = question.Number,
                    Text = question.Text,
                    IsMultipleResponse = question.Choices.Where(c => c.IsCorrect).Count() > 1,

                    Choices = question.Choices.Select((choice, index) => new QuizChoice
                    {
                        Number = choice.Number,
                        Text = choice.Text,
                        IsCorrect = choice.IsCorrect
                    }).ToList(),
                }).ToList()
            };
        }


        /// <summary>
        /// Kacper Bohaczyk
        /// </summary>
        /// <param name="story"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<Quiz> GenerateQuiz(Story story, QuizRequest config)
        {
            ArgumentNullException.ThrowIfNull(story);
            Quiz erg;
            List<ChatMessage> chatMessages;
            try
            {
                chatMessages = RebuildChatMessagesStory(story);
                if (story.Result == null) throw new AIException("The result is not set");
                chatMessages = RebuildChatMessagesResult(story, chatMessages, story.Result.Text);
                chatMessages = RebuildChatMessagesQuiz(story, config, chatMessages);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Failed to rebuild chat messages because of error in story object", e);
            }
            try
            {

                string assistantContent = await FetchAssitantContent(chatMessages, 0.8f, 0.9f);
                erg = GetQuiz(assistantContent);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Failed to rebuild chat messages because of error in story object", e);
            };


            return erg;
            throw new NotImplementedException();
        }
    }

    // Benjamin Edlinger
    /// <summary>
    /// Exception for AI service
    /// </summary>
    public class AIException : Exception
    {
        public AIException()
        {
        }

        public AIException(string message)
            : base(message)
        {
        }

        public AIException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    // Benjamin Edlinger
    public static class ValidRoles
    {
        public const string System = "system";
        public const string User = "user";
        public const string Assistant = "assistant";
    }

    // Benjamin Edlinger
    public class ChatMessage
    {
        private string _role = null!;

        [JsonPropertyName("role")]
        public string Role
        {
            get => _role;
            set
            {
                if (value != ValidRoles.System && value != ValidRoles.User && value != ValidRoles.Assistant)
                {
                    throw new ArgumentException("Invalid role");
                }
                _role = value;
            }
        }

        [JsonPropertyName("content")]
        public string Content { get; set; } = null!;
    }

    // Benjamin Edlinger
    public class Response
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("object")]
        public string Object { get; set; } = null!;

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; } = null!;

        [JsonPropertyName("choices")]
        public List<Choice> Choices { get; set; } = null!;

        [JsonPropertyName("usage")]
        public Usage Usage { get; set; } = null!;
    }

    // Benjamin Edlinger
    public class Choice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("message")]
        public Message Message { get; set; } = null!;

        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; } = null!;
    }

    // Benjamin Edlinger
    public class Message
    {
        private string _role = null!;

        [JsonPropertyName("role")]
        public string Role
        {
            get => _role;
            set
            {
                if (value != ValidRoles.System && value != ValidRoles.User && value != ValidRoles.Assistant)
                {
                    throw new ArgumentException("Invalid role");
                }
                _role = value;
            }
        }

        [JsonPropertyName("content")]
        public string Content { get; set; } = null!;
    }

    // Benjamin Edlinger
    public class StoryContent
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = null!;

        [JsonPropertyName("intertitle")]
        public string Intertitle { get; set; } = null!;

        [JsonPropertyName("story")]
        public string Story { get; set; } = null!;

        [JsonPropertyName("options")]
        public List<Option> Options { get; set; } = null!;
    }

    // Benjamin Edlinger
    public class Option
    {
        [JsonPropertyName("impact")]
        public string ImpactString { get; set; } = null!;

        [JsonIgnore]
        public float Impact => float.Parse(ImpactString, CultureInfo.InvariantCulture);

        [JsonPropertyName("text")]
        public string Text { get; set; } = null!;
    }

    // Benjamin Edlinger
    public class AnalysisContent
    {
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = null!;

        [JsonPropertyName("positive_choices")]
        public string[] PositiveChoices { get; set; } = null!;

        [JsonPropertyName("negative_choices")]
        public string[] NegativeChoices { get; set; } = null!;

        [JsonPropertyName("learnings")]
        public string[] Learnings { get; set; } = null!;

        [JsonPropertyName("discussion_questions")]
        public string[] DiscussionQuestions { get; set; } = null!;
    }

    // Benjamin Edlinger
    public class ImageContent
    {
        [JsonPropertyName("request_id")]
        public string RequestId { get; set; } = null!;

        [JsonPropertyName("inference_status")]
        public InferenceStatus InferenceStatus { get; set; } = null!;

        [JsonPropertyName("images")]
        public List<string> Images { get; set; } = null!;

        [JsonPropertyName("nsfw_content_detected")]
        public List<bool> NsfwContentDetected { get; set; } = null!;

        [JsonPropertyName("seed")]
        public long Seed { get; set; }
    }

    // Benjamin Edlinger
    public class InferenceStatus
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = null!;

        [JsonPropertyName("runtime_ms")]
        public int RuntimeMs { get; set; }

        [JsonPropertyName("cost")]
        public double Cost { get; set; }

        [JsonPropertyName("tokens_generated")]
        public int? TokensGenerated { get; set; }

        [JsonPropertyName("tokens_input")]
        public int? TokensInput { get; set; }
    }

    // Benjamin Edlinger
    public class Usage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }

        [JsonPropertyName("estimated_cost")]
        public double EstimatedCost { get; set; }
    }
}


// Kacper Bohaczyk
public class QuizContent
{
    [JsonPropertyName("Title")]
    public string Title { get; set; } = null!;

    [JsonPropertyName("NumberQuestions")]
    public uint NumberQuestions { get; set; }

    [JsonPropertyName("Questions")]
    public List<Questions> Questions { get; set; } = null!;

}

public class Questions
{
    [JsonPropertyName("Text")]
    public string Text { get; set; } = null!;

    [JsonPropertyName("Number")]
    public int Number { get; set; }

    [JsonPropertyName("IsMultipleResponse")]
    public Boolean IsMultipleResponse { get; set; }

    [JsonPropertyName("Choices")]
    public List<Choices> Choices { get; set; } = null!;
}

public class Choices
{
    [JsonPropertyName("Text")]
    public string Text { get; set; } = null!;

    [JsonPropertyName("Number")]
    public int Number { get; set; }

    [JsonPropertyName("IsCorrect")]
    public Boolean IsCorrect { get; set; }



}