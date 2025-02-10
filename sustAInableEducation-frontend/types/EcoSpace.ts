export interface Participant {
    userId: string
    userName: string
    isHost: boolean,
    isOnline: boolean,
    impact: number
}

export interface Choice {
    number: number,
    text: string,
    numberVotes: number
}

export interface Part {
    intertitle: string,
    text: string,
    votingEndAt: string,
    chosenNumber: number,
    choices: Array<Choice>,
    image: string
}

export interface Result {
    text: string,
    summary: string,
    positiveChoices: Array<string>,
    negativeChoices: Array<string>,
    learnings: Array<string>,
    discussionQuestions: Array<string>,
}

export interface Story {
    title: string,
    prompt: string,
    length: number,
    temperature: number,
    topP: number,
    totalImpact: number,
    targetGroup: number,
    parts: Array<Part>,
    result: Result | null,
}

export interface EcoSpace {
    id: string,
    votingTimeSeconds: number,
    createdAt: string,
    participants: Array<Participant>,
    story:  Story
}