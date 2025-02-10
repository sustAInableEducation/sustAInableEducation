<template>
    <div class="w-full h-screen flex items-center justify-center">
        <div class="background animate-anim" />
        <div class="w-screen flex flex-col items-center justify-center h-full">
            <div
                class="bg-slate-50 shadow-xl rounded-xl flex flex-col p-4 items-center w-full max-w-[1000px] h-full max-h-[650px] ">
                <div class="h-full max-h-[570px] flex flex-col items-center w-full">
                    <h1 class="text-3xl font-bold mb-8 h-5">EcoSpace erstellen</h1>
                    <Stepper value="1" class="flex-1 w-full h-full" linear>
                        <Steplist>
                            <Divider />
                            <Step value="1">Thema</Step>
                            <Divider />
                            <Step value="2">Einstellungen</Step>
                            <Divider />
                        </Steplist>
                        <StepPanels class="!p-0 w-full h-full max-h-[520px]">
                            <StepPanel v-slot="{ activateCallback }" value="1" class="w-full h-full rounded-xl">
                                <Tabs value="0" class="w-full h-full flex !rounded-xl">
                                    <TabList class="flex flex-none">
                                        <Tab value="0" as="div"
                                            class="h-10 flex-1 flex justify-center items-center !bg-white !rounded-tl-xl">
                                            <span>SDGs</span>
                                        </Tab>
                                        <Tab value="1" as="div"
                                            class="h-10 flex-1 flex justify-center items-center !bg-white !rounded-tr-xl">
                                            <span>Eigenes Thema</span>
                                        </Tab>
                                    </TabList>
                                    <TabPanels class="!p-4 w-full flex-1 h-full rounded-xl">
                                        <TabPanel value="0"
                                            class="flex flex-wrap flex-col justify-between h-full w-full">
                                            <Panel class="h-full max-h-[375px] w-full overflow-y-scroll">
                                                <div class="w-full flex flex-wrap justify-center">
                                                    <img v-for="sdg in sdgAssets" :src="sdg.asset_path" :alt="sdg.name"
                                                        class="w-40 h-40 m-2 cursor-pointer"
                                                        :class="{ 'box-border border-4 border-white': (sdg.id === selectedSdg) }"
                                                        @click="selectSdg(sdg.id)" />
                                                </div>
                                            </Panel>
                                            <div class="w-full">
                                                <Divider />
                                                <div class="flex items-center justify-end "
                                                    :class="{ 'justify-between': selectedSdg != -1 }">
                                                    <p v-if="selectedSdg != -1"> Ausgewählt: {{
                                                        getSdgAsset(selectedSdg)?.name }}</p>
                                                    <Button label="Weiter"
                                                        v-tooltip.bottom="{ value: (selectedSdg === -1) ? 'Es muss eine Auswahl getroffen werden' : null }"
                                                        :disabled="selectedSdg === -1"
                                                        @click="goToNextStep(activateCallback, 'sdg')" />
                                                </div>
                                            </div>
                                        </TabPanel>
                                        <TabPanel value="1"
                                            class="h-full w-full">
                                            <Panel class="w-full h-full max-h-[375px]">
                                                <div class="text-xl flex w-full">
                                                    <div class="flex flex-col w-full sm:w-fit">
                                                        <div class="flex flex-col">
                                                            <div class="flex items-center">
                                                                <div class="flex flex-col items-start sm:flex-row sm:items-center">
                                                                    <span class="flex flex-wrap w-full">
                                                                        Das Thema, welches die Geschichte behandeln soll,
                                                                        ist das
                                                                        <Button class="!text-xl !p-0 sm:!hidden" @click="focusTextareaMobile"
                                                                            label="Thema" variant="link" />
                                                                        <Button class="!text-xl !p-0 !mx-1 !hidden sm:!block" @click="focusTextarea"
                                                                            label="Thema" variant="link" />
                                                                    </span>
                                                                    <Textarea id="descriptionMobile" class="block my-2 resize-none max-h-[175px] sm:hidden"
                                                                        v-model="customTopic" rows="1" cols="20"
                                                                        autoResize />
                                                                </div>
                                                            </div>
                                                            <span>
                                                                und dieses Thema soll folgende Punkte
                                                                thematisieren:
                                                            </span>
                                                        </div>
                                                        <ul class="list-decimal ml-10 flex flex-col max-w-96 mt-2">
                                                            <li v-for="ref, index in bulletPoints" class="mb-2">
                                                                <div class="flex items-center justify-between">
                                                                    <Textarea class="w-fit" v-model="ref.value" rows="1"
                                                                        cols="50" autoResize />
                                                                    <div v-if="bulletPoints.length != 1"
                                                                        @click="removeBulletPoint(index)"
                                                                        class="ml-2 rounded-full border-2 border-solid border-red-500 size-fit flex justify-center cursor-pointer">
                                                                        <Icon name="ic:baseline-minus"
                                                                            class="bg-red-500 size-5" />
                                                                    </div>
                                                                </div>
                                                            </li>
                                                            <Button class="!p-0" rounded @click="addBulletPoint()"
                                                                v-if="bulletPoints.length < 3">
                                                                <Icon name="ic:baseline-plus"
                                                                    class="bg-white mx-4 size-6" />
                                                            </Button>
                                                        </ul>
                                                    </div>
                                                    <div class="flex-1">
                                                        <Textarea id="description" class="my-2 resize-none max-h-[175px] hidden sm:block sm:m-0 sm:ml-2"
                                                            v-model="customTopic" rows="1" cols="20"
                                                            autoResize />
                                                    </div>
                                                </div>
                                            </Panel>
                                            <div class="w-full">
                                                <Divider />
                                                <div class="flex items-center justify-end">
                                                    <Button label="Weiter" v-tooltip.bottom="{ value: topicTooltip }"
                                                        :disabled="!topicFilledOut"
                                                        @click="goToNextStep(activateCallback, 'custom')" />
                                                </div>
                                            </div>
                                        </TabPanel>
                                    </TabPanels>
                                </Tabs>

                            </StepPanel>
                            <StepPanel v-slot="{ activateCallback }" value="2" class="w-full h-full max-h-[520px]">
                                <div class="flex flex-col justify-between h-full  max-h-[520px] p-4 bg-slate-50">
                                    <Panel header="Weitere Konfiguration" class="">
                                        <div class="flex flex-col justify-between h-full">
                                            <Form class="flex flex-col h-full">
                                                <div class="flex flex-col mt-4">
                                                    <label for="entscheidungspunkte"
                                                        class="mb-1 text-lg">Entscheidungspunkte</label>
                                                    <InputNumber id="entscheidungspunkte" v-model="decisionPoints"
                                                        :min="3" :max="10" showButtons />
                                                </div>
                                                <div class="flex flex-col mt-4">
                                                    <label for="zielgruppe" class="mb-1 text-lg">Zielgruppe</label>
                                                    <Select id="zielgruppe" :options="zielgruppen"
                                                        v-model="selectedZielgruppe"
                                                        placeholder="Wählen Sie eine Zielgruppe" />
                                                </div>
                                                <div class="flex flex-col mt-4">
                                                    <label for="voteTime" class="mb-1 text-lg">Abstimmungszeit</label>
                                                    <InputNumber id="voteTime" v-model="voteTime" :min="10"
                                                        suffix=" Sekunden" :max="30" showButtons />
                                                </div>
                                            </Form>
                                            <Message severity="info" class="justify-between mt-6">
                                                <div class="flex justify-between items-center">
                                                    <div>
                                                        <span class="font-bold">Zeitschätzung: </span>
                                                        <span>{{ estimatedTime }}</span>
                                                    </div>
                                                    <Icon name="ic:twotone-info" class="size-6 cursor-pointer"
                                                        @click="showEstimatedTimeDialog = true" />
                                                </div>
                                            </Message>
                                            <Dialog v-model:visible="showEstimatedTimeDialog" modal
                                                header="Berechnung der Zeitschätzung" class="w-full max-w-[650px] mx-4">
                                                <div class="flex flex-col">
                                                    <p>Die Zeitschätzung gibt einen groben Richtwert für die Dauer eines
                                                        EcoSpaces an.
                                                    </p>
                                                    <p>
                                                        Sie wird anhand der ausgewählten Einstellungen
                                                        (Anzahl der Entschiedungspunkte, Abstimmungszeit) berechnet.
                                                    </p>
                                                </div>
                                            </Dialog>
                                        </div>
                                    </Panel>
                                    <div class="w-full">
                                        <Divider />
                                        <div class="flex items-center justify-between">
                                            <Button label="Zurück" severity="secondary"
                                                @click="activateCallback('1')" />
                                            <Button label="EcoSpace erstellen"
                                                v-tooltip.bottom="{ value: configurationTooltip }"
                                                :disabled="!configFilledOut || loading"
                                                @click="createSpace()"
                                                :loading="loading" />
                                        </div>
                                    </div>
                                </div>
                            </StepPanel>
                        </StepPanels>
                    </Stepper>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { Configuration } from '~/types/configuration';
import { SdgAsset } from '~/types/sdgs';

useHead({
    title: 'EcoSpace erstellen - sustAInableEducation'
})

const runtimeConfig = useRuntimeConfig()
const route = useRoute()

const loading = ref(false)

// Refs
const topicType = ref('sdg')
const selectedSdg = ref(-1)
const customTopic = ref('')
const bulletPoints = ref([ref('')])
const decisionPoints = ref(3)
const selectedZielgruppe = ref('')
const voteTime = ref(10)

const showEstimatedTimeDialog = ref(false)

const sdgAssets = ref(Object.fromEntries(
    Object.entries(SdgAsset.sdgs).map(([key, { iconPath, gifPath, ...rest }]) => [
        key,
        {
            ...rest,
            asset_path: iconPath
        }
    ])
))


// Other Variables
const zielgruppen = ['Volksschule (6-10 Jahre)', 'Sekundarstufe I (11-14 Jahre)', 'Sekundarstufe II (15-19 Jahre)',]

onMounted(() => {
    isLoggedInRequest()
})

// Computed Properties
const topicFilledOut = computed(() => {
    return customTopic.value.length > 0 && bulletPoints.value.every(ref => ref.value.length > 0)
})

const configFilledOut = computed(() => {
    return decisionPoints.value >= 3 && decisionPoints.value <= 10 && selectedZielgruppe.value.length > 0 && voteTime.value >= 10 && voteTime.value <= 30
})

const estimatedTime = computed(() => {
    let seconds = (decisionPoints.value - 1) * voteTime.value + decisionPoints.value * 90
    return `${Math.trunc(seconds / 60)} Minuten` + (seconds % 60 !== 0 ? ` ${seconds % 60} Sekunden` : '')
})

const topicTooltip = computed(() => {
    if (customTopic.value.length === 0) {
        return 'Das Thema muss ausgefüllt werden'
    }
    if (bulletPoints.value.some(ref => ref.value.length === 0)) {
        return 'Alle Punkte müssen ausgefüllt werden'
    }
    return null
})

const configurationTooltip = computed(() => {
    if (decisionPoints.value < 3) {
        return 'Es müssen mindestens 3 Entscheidungspunkte ausgewählt werden'
    }
    if (decisionPoints.value > 10) {
        return 'Es können maximal 10 Entscheidungspunkte ausgewählt werden'
    }
    if (selectedZielgruppe.value.length === 0) {
        return 'Die Zielgruppe muss ausgewählt werden'
    }
    if (voteTime.value < 10) {
        return 'Die Abstimmungszeit muss mindestens 10 Sekunden betragen'
    }
    if (voteTime.value > 30) {
        return 'Die Abstimmungszeit kann maximal 30 Sekunden betragen'
    }
    return null
})


// Functions
function goToNextStep(activateCallback: (step: string) => void, topic: string) {
    topicType.value = topic
    activateCallback('2')
}


function getSdgAsset(id: number) {
    return Object.values(sdgAssets.value).find(sdg => sdg.id === id)
}

function selectSdg(newSdg: number) {
    if (selectedSdg.value != -1) {
        let prev = getSdgAsset(selectedSdg.value)
        console.log(prev)
        prev!.asset_path = Object.values(SdgAsset.sdgs).find(sdg => sdg.id === selectedSdg.value)!.iconPath
    }
    if (selectedSdg.value === newSdg) {
        selectedSdg.value = -1
        return
    } else {
        let next = getSdgAsset(newSdg)
        next!.asset_path = Object.values(SdgAsset.sdgs).find(sdg => sdg.id === newSdg)!.gifPath
        selectedSdg.value = newSdg
    }
}

function focusTextarea() {
    const textarea = document.getElementById('description') as HTMLTextAreaElement
    textarea.focus()
}
function focusTextareaMobile() {
    const textarea = document.getElementById('descriptionMobile') as HTMLTextAreaElement
    textarea.focus()
}

function addBulletPoint() {
    bulletPoints.value.push(ref(''))
}

function removeBulletPoint(index: number) {
    bulletPoints.value.splice(index, 1)
}

function createSpace() {
    let targetGroupNum = 0
    switch (selectedZielgruppe.value) {
        case 'Volksschule (6-10 Jahre)':
            targetGroupNum = 0
            break
        case 'Sekundarstufe I (11-14 Jahre)':
            targetGroupNum = 1
            break
        case 'Sekundarstufe II (15-19 Jahre)':
            targetGroupNum = 2
            break
    }


    if (topicType.value === 'sdg') {
        createSpaceFromSdg(targetGroupNum)
    } else {
        createSpaceFromTopic(targetGroupNum)
    }
}

function isLoggedInRequest() {
    $fetch(`${runtimeConfig.public.apiUrl}/account`, {
        method: 'GET',
        credentials: 'include',
        onResponse: (response) => {
            if (response.response.status === 401) {
                navigateTo('/login?redirect=' + route.fullPath)
            }
        }
    })
}

function createSpaceFromSdg(targetGroup: number) {

    loading.value = true

    $fetch(`${runtimeConfig.public.apiUrl}/spaces`, {
        method: 'POST',
        credentials: 'include',
        body: {
            votingTimeSeconds: voteTime.value,
            story: {
                topic: Configuration.generatePromptFromSdg(selectedSdg.value),
                targetGroup: targetGroup,
                length: decisionPoints.value,
                temperature: Configuration.defaultTemperature,
                topP: Configuration.defaultTopP
            }
        },
        onResponse: (response) => {
            if (response.response.ok) {
                navigateTo(`/spaces/${response.response._data.id}`)
            } else {
                loading.value = false;
                console.error(response.error)
            }
        }
    })
}

function createSpaceFromTopic(targetGroup: number) {

    loading.value = true

    $fetch(`${runtimeConfig.public.apiUrl}/spaces`, {
        method: 'POST',
        credentials: 'include',
        body: {
            votingTimeSeconds: voteTime.value,
            story: {
                topic: Configuration.generatePromptFromTopic(customTopic.value, bulletPoints.value.map(ref => ref.value)),
                targetGroup: targetGroup,
                length: decisionPoints.value,
                temperature: Configuration.defaultTemperature,
                topP: Configuration.defaultTopP
            }
        },
        onResponse: (response) => {
            if (response.response.ok) {
                navigateTo(`/spaces/${response.response._data.id}`)
            } else {
                loading.value = false;
                console.error(response.error)
            }
        }
    })
}
</script>