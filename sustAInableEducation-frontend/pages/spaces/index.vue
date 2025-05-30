<template>
    <div class="w-full h-full">
        <div class="w-screen flex items-center h-full bg-slate-50 relative">
            <Toast />
            <ConfirmDialog></ConfirmDialog>
            <div class="sidebar-container w-80 h-full pt-16 border-solid border-slate-300 border-r-2 hidden sm:block">
                <div class="sidebar w-full h-full flex-col p-2 overflow-y-scroll flex">
                    <div id="sidebar-header">
                        <div class="flex items-center">
                            <IconField class="mr-2">
                                <InputIcon>
                                    <Icon name="ic:baseline-search" />
                                </InputIcon>
                                <InputText placeholder="Suchen" v-model="searchInput" class="w-full" />
                            </IconField>
                            <Button class="!aspect-square !p-0 !" @click="toggleShowFilters">
                                <template #default>
                                    <div class="flex items-center justify-center size-11">
                                        <Icon :name="!showFilters ? 'ic:baseline-filter-alt' : 'ic:baseline-close'"
                                            :class="[showFilters ? 'size-6' : 'size-4']" />
                                    </div>
                                </template>
                            </Button>
                        </div>
                        <Panel class="mt-2" v-if="showFilters">
                            <div>
                                <h3 class="text-lg font-bold">Filter</h3>
                                <Divider />
                            </div>
                            <div class="flex flex-col mb-4">
                                <label class="mb-1" for="sortSelect">Sortierrichtung:</label>
                                <div class="flex">
                                    <Select class="w-32 flex-1 mr-1" id="sortSelect" size="small"
                                        v-model="filters.refs.sort.subject.value" :options="filters.options.sort" />
                                    <ToggleButton class="w-fit aspect-square !p-2"
                                        v-model="filters.refs.sort.direction.value">
                                        <template #default>
                                            <Icon v-if="filters.refs.sort.direction.value"
                                                name="ic:baseline-arrow-upward" />
                                            <Icon v-else name="ic:baseline-arrow-downward" />
                                        </template>
                                    </ToggleButton>
                                </div>
                            </div>
                            <div class="flex flex-col items-start mb-4">
                                <label class="text-md mb-1" for="finishedSelect">Abgeschlossen:</label>
                                <Select class="w-full" id="finishedSelect" size="small"
                                    v-model="filters.refs.finished.value" :options="filters.options.finished" />
                            </div>
                            <div class="flex flex-col items-start">
                                <label class="mb-1" for="datePicker">Erstellungsdatum:</label>
                                <DatePicker class="w-full" id="datePicker" v-model="filters.refs.date.value"
                                    size="small" dateFormat="dd.mm.yy" showButtonBar selectionMode="range"
                                    :manualInput="false" />
                            </div>
                            <Message v-if="!isFilterApplied" class="mt-4">
                                Die ausgewählten Filter wurden noch nicht angewandt.
                            </Message>
                            <Divider />
                            <div class="flex justify-between">
                                <Button label="Anwenden" size="small" @click="applyFilters()" />
                                <Button label="Zurücksetzen" size="small" variant="text" @click="resetFilters()" />

                            </div>
                        </Panel>
                        <Divider class="!w-full" />
                    </div>
                    <div id="sidebar-content">
                        <EcoSpaceListEntry v-for="ecoSpace in sortedSpaces" :ecoSpace="ecoSpace" :key="ecoSpace.id"
                            :show-delete="myUserId === ecoSpace.participants.find(participant => participant.isHost)?.userId"
                            v-on:delete="openDialog" v-model="spaceRefsById[ecoSpace.id].value"
                            v-on:click="navigateTo({ path: '/spaces', query: { spaceId: ecoSpace.id } });" />
                        <NuxtLink to="/configuration">
                            <Button label="EcoSpace erstellen" rounded size="small" class="w-full !text-">
                                <template #icon>
                                    <Icon name="ic:baseline-add" class="size-5" />
                                </template>
                            </Button>
                        </NuxtLink>
                        <div v-if="spaces?.length === 0 || !spaces" class="mt-2">
                            <Message class="text-md flex justify-center items-center w-full">
                                Es gibt noch keine EcoSpaces
                            </Message>
                        </div>
                    </div>
                </div>
            </div>

            <MobileSidebar class="sm:hidden" v-model="showSidebar" :searched-spaces="searchedSpaces"
                :search-input="searchInput" :show-filters="showFilters" :filters="filters"
                :is-filter-applied="isFilterApplied" :spaces="spaces" :selected-space="selectedSpace"
                @toggle-filters="toggleShowFilters" :space-refs-by-id="spaceRefsById" @apply-filters="applyFilters"
                @reset-filters="resetFilters" @open-delete-dialog="openDialog" @select-space="selectSpaceCloseSidebar"
                @toggle-sidebar="showSidebar = !showSidebar" @search-update="updateSearch" />

            <div class="content flex-1 h-full overflow-y-scroll">
                <div v-if="selectedSpace" class="w-full pt-20 p-4">
                    <div class="flex items-start flex-col w-full h-full">
                        <div class="w-full mb-4 justify-between flex flex-wrap">
                            <h1 class="text-4xl font-bold">{{ selectedSpace.story.title }}</h1>
                            <Button v-if="!ecoSpaceIsFinished(selectedSpace)" label="EcoSpace beitreten"
                                @click="navigateTo('/spaces/' + selectedSpace.id)" />
                        </div>
                        <Panel header="Informationen" class="w-full mb-4">
                            <Divider />
                            <div class="w-full h-full flex justify-between">
                                <div class="flex flex-col justify-between flex-1 sm:mr-40 w-full">
                                    <div class="text-lg">
                                        <p class="mb-2">
                                            <span class="font-bold">Erstellt am</span>
                                            {{ formatDate(selectedSpace.createdAt) }}
                                        </p>
                                        <p class="mb-2">
                                            <span class="font-bold">Anzahl der Entscheidungspunkte:</span>
                                            {{ selectedSpace.story.length }}
                                        </p>
                                        <p class="mb-2">
                                            <span class="font-bold">Abstimmungszeit:</span>
                                            {{ selectedSpace.votingTimeSeconds + ' Sekunden' }}
                                        </p>
                                        <p class="mb-2">
                                            <span class="font-bold">Zielgruppe:</span>
                                            {{ getTargetgroup(selectedSpace.story.targetGroup) }}
                                        </p>
                                    </div>
                                    <div class="mb-4">
                                        <MeterGroup :value="getProgressData()" labelPosition="start"
                                            v-tooltip.bottom="{ value: getProgressLabel(), showDelay: 50 }">
                                            <template #label="{ totalPercent }">
                                                <p v-if="totalPercent < 100">Zu {{ totalPercent }}% Abgeschlossen
                                                </p>
                                                <p v-else class="flex items-center">
                                                    Abgeschlossen
                                                    <Icon name="ic:baseline-check" class="ml-2 size-5" />
                                                </p>
                                            </template>
                                        </MeterGroup>
                                    </div>
                                    <Fieldset legend="Teilnehmer" class="max-h-64 overflow-scroll sm:hidden">
                                        <div class="!max-w-full">
                                            <DataTable :value="selectedSpace.participants"
                                                class="!bg-green-300 !flex !max-w-">
                                                <Column field="userName" header="Benutzername">
                                                    <template #body="{ data }">
                                                        <div class="flex items-center">
                                                            <div class="size-5 mr-1">
                                                                <Icon name="ic:baseline-star-rate"
                                                                    class="size-5 bg-yellow-500"
                                                                    v-tooltip.bottom="{ value: 'Host', showDelay: 50 }"
                                                                    v-if="data.isHost" />
                                                            </div>
                                                            <span
                                                                :class="data.userId === myUserId ? 'underline decoration-2 font-bold' : ''">
                                                                {{ data.userName }}
                                                            </span>
                                                        </div>
                                                    </template>
                                                </Column>
                                                <Column field="impact" header="Einfluss">
                                                    <template #body="{ data }">
                                                        <div class="flex items-center justify-center">
                                                            <span v-if="ecoSpaceIsFinished(selectedSpace)">{{
                                                                data.impact
                                                                }}</span>
                                                            <span v-else>?</span>
                                                        </div>
                                                    </template>
                                                </Column>
                                            </DataTable>
                                        </div>
                                    </Fieldset>
                                </div>
                                <Fieldset legend="Teilnehmer" class="max-h-64 overflow-scroll hidden sm:block">
                                    <div class="h-full w-full">
                                        <DataTable :value="selectedSpace.participants">
                                            <Column field="userName" header="Benutzername">
                                                <template #body="{ data }">
                                                    <div class="flex items-center">
                                                        <div class="size-5 mr-1">
                                                            <Icon name="ic:baseline-star-rate"
                                                                class="size-5 bg-yellow-500"
                                                                v-tooltip.bottom="{ value: 'Host', showDelay: 50 }"
                                                                v-if="data.isHost" />
                                                        </div>
                                                        <span
                                                            :class="data.userId === myUserId ? 'underline decoration-2 font-bold' : ''">{{
                                                                data.userName }}</span>
                                                    </div>
                                                </template>
                                            </Column>
                                            <Column field="impact" header="Einfluss">
                                                <template #body="{ data }">
                                                    <div class="flex items-center justify-center">
                                                        <span v-if="ecoSpaceIsFinished(selectedSpace)">{{ data.impact
                                                        }}</span>
                                                        <span v-else>?</span>
                                                    </div>

                                                </template>
                                            </Column>
                                        </DataTable>
                                    </div>
                                </Fieldset>
                            </div>
                        </Panel>
                        <div class="flex justify-between items-center w-full mb-2">
                            <h2 class="text-2xl">Storyteile</h2>
                            <InputGroup class="!w-fit">
                                <Button label="Alle einklappen" @click="collapseAccordion" size="small"
                                    class="!mr-[1px]" />
                                <Button label="Alle aufklappen" @click="decollapseAccordion"
                                    size="small" />
                            </InputGroup>
                        </div>
                        <Accordion :value="openedAccordions" multiple class="w-full my-2">
                            <AccordionPanel v-for="part, index in selectedSpace.story.parts" :key="part.intertitle"
                                :value="index">
                                <AccordionHeader class="!text-xl">{{ part.intertitle }}</AccordionHeader>
                                <AccordionContent>
                                    <p class="m-0 mb-2">{{ part.text }}</p>
                                    <h3 class="text-lg font-bold mb-2">Optionen</h3>
                                    <div class="flex flex-col">
                                        <Button v-for="choice in part.choices" class="!w-full !mb-2">
                                            <template #default>
                                                <div class="flex w-full items-center">
                                                    <div class="flex items-center size-5 mr-3">
                                                        <Icon name="ic:baseline-check" class="size-5"
                                                            v-if="choice.number === part.chosenNumber" />
                                                    </div>
                                                    <div class="flex justify-between w-full items-center">
                                                        <span>{{ choice.text }}</span>
                                                        <Chip class="!bg-white !text-black h-fit !p-1 !px-2 text-sm"
                                                            :label="choice.numberVotes.toString() + ' Stimmen'" />
                                                    </div>
                                                </div>
                                            </template>
                                        </Button>
                                    </div>
                                </AccordionContent>
                            </AccordionPanel>
                            <h2 class="text-xl mt-4 mb-2" v-if="ecoSpaceIsFinished(selectedSpace)">Ergebnis</h2>
                            <AccordionPanel v-if="ecoSpaceIsFinished(selectedSpace)"
                                :value="selectedSpace.story.parts.length">
                                <AccordionHeader>{{ selectedSpace.story.result!.text }}</AccordionHeader>
                                <AccordionContent>
                                    <p>{{ selectedSpace.story.result!.summary }}</p>
                                </AccordionContent>
                            </AccordionPanel>
                        </Accordion>
                    </div>
                </div>
                <div v-else class="pt-20 w-full h-full flex items-center justify-center">
                    <p class="text-lg">Bitte wählen Sie einen EcoSpace aus der Liste aus.</p>

                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import type { EcoSpace } from '~/types/EcoSpace';
import type { OverviewFilter } from '~/types/filter';

useHead({
    title: 'EcoSpace Übersicht - sustAInableEducation'
})

const runtimeConfig = useRuntimeConfig();
const confirmDialog = useConfirm();
const toast = useToast();

const route = useRoute();
const router = useRouter();

const { execute, data: spaces } = await useFetch<EcoSpace[]>(`${runtimeConfig.public.apiUrl}/spaces`,
    {
        method: 'GET',
        cache: 'no-cache',
        credentials: 'include',
        headers: useRequestHeaders(['cookie']),
        onResponse: (response) => {
            if (response.response.status === 401) {
                navigateTo('/login?redirect=' + route.fullPath);
            }
        }
    }
)

const openedAccordions = ref<number[]>([]);

const showFilters = ref(false);

const showSidebar = ref(true);

const filters: OverviewFilter = {
    applied: {
        finished: ref('Alle'),
        date: ref<Date | Date[] | (Date | null)[] | null | undefined>(undefined),
        sort: {
            subject: ref('Erstellungsdatum'),
            direction: ref(true) // false = ascending, true = descending
        }
    },
    refs: {
        finished: ref('Alle'),
        date: ref<Date | Date[] | (Date | null)[] | null | undefined>(),
        sort: {
            subject: ref('Erstellungsdatum'),
            direction: ref(false) // false = ascending, true = descending
        }
    },
    options: {
        finished: [
            'Alle',
            'Beendet',
            'Nicht beendet'
        ],
        sort: [
            'Erstellungsdatum',
            'Titel',
            'Anzahl der Entscheidungspunkte',
            'Anzahl der Teilnehmer'
        ]
    }
};

const myUserId = ref('')

await getUser()

const spaceRefsById = spaces.value ? spaces.value.reduce((acc, space) => {
    acc[space.id] = ref(false);
    return acc;
}, {} as Record<string, Ref<boolean>>) : {};

if (route.query.spaceId && spaces) {
    selectSpaceById(route.query.spaceId as string);
}

watch(() => route.query.spaceId, (newVal) => {
    if (newVal && spaces) {
        selectSpaceById(newVal as string);
    }
});

const selectedSpace = ref<EcoSpace>();

const searchInput = ref('');

const filteredSpaces = computed<EcoSpace[]>(() => {
    if (!spaces.value) return [];

    const normalizeDate = (date: Date) => {
        return new Date(date.getFullYear(), date.getMonth(), date.getDate());
    }

    return spaces.value.filter(space => {
        let finished;
        switch (filters.applied.finished.value) {
            case 'Alle':
                finished = true;
                break;
            case 'Beendet':
                finished = ecoSpaceIsFinished(space);
                break;
            case 'Nicht beendet':
                finished = !ecoSpaceIsFinished(space);
                break;
        }

        if (filters.applied.date.value) {
            if (Array.isArray(filters.applied.date.value) && filters.applied.date.value[0] !== null) {
                let fromDate = normalizeDate(new Date(space.createdAt)) >= filters.applied.date.value[0];
                if (filters.applied.date.value[1] !== null) {
                    let toDate = normalizeDate(new Date(space.createdAt)) <= filters.applied.date.value[1];
                    return finished && fromDate && toDate;
                } else {
                    return finished && fromDate;
                }
            }
        }
        return finished;
    });
});

const searchedSpaces = computed<EcoSpace[]>(() => {
    return filteredSpaces.value.filter(space => {
        if (space.story.title) {
            return space.story.title.toLowerCase().includes(searchInput.value.toLowerCase());
        }
        return searchInput.value === "";
    });
});

const sortedSpaces = computed<EcoSpace[]>(() => {
    return [...searchedSpaces.value].sort((a, b) => {
        switch (filters.applied.sort.subject.value) {
            case 'Erstellungsdatum':
                return filters.applied.sort.direction.value
                    ? new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
                    : new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
            case 'Titel':
                return filters.applied.sort.direction.value
                    ? b.story.title.localeCompare(a.story.title)
                    : a.story.title.localeCompare(b.story.title);
            case 'Anzahl der Entscheidungspunkte':
                return filters.applied.sort.direction.value
                    ? a.story.length - b.story.length
                    : b.story.length - a.story.length;
            case 'Anzahl der Teilnehmer':
                return filters.applied.sort.direction.value
                    ? a.participants.length - b.participants.length
                    : b.participants.length - a.participants.length;
            default:
                return 0;
        }
    });
});


const isFilterApplied = computed(() => {
    return filters.applied.finished.value === filters.refs.finished.value && filters.applied.date.value === filters.refs.date.value && filters.applied.sort.subject.value === filters.refs.sort.subject.value && filters.applied.sort.direction.value === filters.refs.sort.direction.value;
})

async function selectSpaceById(id: string) {
    spaceRefsById[id].value = true;
    if (spaces.value) {
        await $fetch(`${runtimeConfig.public.apiUrl}/spaces/${id}`, {
            method: 'GET',
            credentials: 'include',
            onResponse: (response) => {
                if (response.response.ok) {
                    selectedSpace.value = response.response._data;
                    if (selectedSpace.value) {
                        openedAccordions.value = Array.from(Array(selectedSpace.value.story.parts.length).keys())
                    }
                }
            }
        });
    }
    Object.keys(spaceRefsById).forEach(key => {
        if (key !== id) {
            spaceRefsById[key].value = false;
        }
    });
}

async function selectSpaceCloseSidebar(id: string) {
    navigateTo({
        path: '/spaces',
        query: {
            spaceId: id
        }
    });
    showSidebar.value = false;
}

function formatDate(dateString: string) {
    const date = new Date(dateString);
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    return `${day}.${month}.${year}`;
}

function ecoSpaceIsFinished(space: EcoSpace): boolean {
    return !!space.story.result;
}

function getProgressData() {
    let divider = selectedSpace.value!.story.result ? selectedSpace.value!.story.length : selectedSpace.value!.story.length+1;
    let percentage = (selectedSpace.value!.story.parts.length / divider) * 100;
    return [{ label: '', value: percentage, color: 'var(--p-primary-color)' }];
}

function getProgressLabel() {
    let relative = selectedSpace.value!.story.parts.length / selectedSpace.value!.story.length;
    if (relative < 1) {
        return `${selectedSpace.value!.story.parts.length} von ${selectedSpace.value!.story.length} Entscheidungspunkten wurden abgeschlossen`;
    }
    return `Alle ${selectedSpace.value!.story.length} Entscheidungspunkten wurden abgeschlossen`;
}

function updateSearch(newVal: string) {
    searchInput.value = newVal;
}

function toggleShowFilters() {
    showFilters.value = !showFilters.value;
}

function resetFilters() {
    filters.refs.finished.value = 'Alle';
    filters.refs.date.value = undefined;
    filters.refs.sort.subject.value = 'Erstellungsdatum';
    filters.refs.sort.direction.value = false;
}

function applyFilters() {
    filters.applied.finished.value = filters.refs.finished.value;
    filters.applied.date.value = filters.refs.date.value;
    filters.applied.sort.subject.value = filters.refs.sort.subject.value;
    filters.applied.sort.direction.value = filters.refs.sort.direction.value;
}

function deleteSpace(id: string) {
    $fetch(`${runtimeConfig.public.apiUrl}/spaces/${id}`, {
        method: 'DELETE',
        credentials: 'include',
        onResponse: (response) => {
            if (response.response.ok) {
                toast.add({
                    severity: 'success',
                    life: 5000,
                    summary: 'Erfolgreich gelöscht',
                    detail: `Der EcoSpace, mit der ID ${id}, wurde erfolgreich gelöscht.`,
                });
                execute();
            } else {
                toast.add({
                    severity: 'error',
                    life: 5000,
                    summary: 'Fehler beim Löschen',
                    detail: `Der EcoSpace, mit der ID ${id}, konnte nicht gelöscht werden.`
                });
            }
        }
    });
}

const openDialog = (id: string) => {
    confirmDialog.require({
        message: 'Sind Sie sich sicher, dass Sie diesen EcoSpace löschen möchten?',
        header: 'Endgültig Löschen',
        rejectProps: {
            label: 'Abbrechen',
            severity: 'secondary',
            outlined: true
        },
        acceptProps: {
            label: 'Löschen',
            severity: 'danger'
        },
        accept: () => {
            deleteSpace(id);
        },
        reject: () => {

        }
    });
}

function getTargetgroup(targetGroup: number) {
    switch (targetGroup) {
        case 0:
            return 'Volksschule';
        case 1:
            return 'Sekundarstufe I';
        default:
            return 'Sekundarstufe II';
    }
}

function getUser() {
    $fetch(`${runtimeConfig.public.apiUrl}/account`, {
        method: 'GET',
        credentials: 'include',
        onResponse: (response) => {
            if (response.response.ok) {
                myUserId.value = response.response._data.id;
            } else if (response.response.status === 401) {
                router.push('/login?redirect=' + route.fullPath);
            }
        }
    });
}

function collapseAccordion() {
    openedAccordions.value = [];
}

function decollapseAccordion() {
    openedAccordions.value = Array.from(Array(selectedSpace.value!.story.parts.length).keys());
}
</script>

<style scoped></style>