<template>
    <div class="w-full h-full">
        <div class="w-screen flex items-center h-full bg-slate-50">
            <Toast />
            <ConfirmDialog></ConfirmDialog>
            <div class="w-80 h-full pt-16 border-solid border-slate-300 border-r-2">
                <div class="sidebar w-full h-full flex flex-col p-2 overflow-y-scroll">
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
                        <EcoSpaceListEntry v-for="ecoSpace in searchedSpaces" :ecoSpace="ecoSpace"
                            v-on:delete="openDialog" v-model="spaceRefsById[ecoSpace.id].value"
                            v-on:click="selectSpaceById(ecoSpace.id)" />
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
            <div class="content flex-1 h-full overflow-y-scroll">
                <div v-if="selectedSpace" class="w-full pt-20 p-4">
                    <div class="flex items-start flex-col w-full h-full">
                        <h1 class="text-4xl font-bold mb-4">{{ selectedSpace.story.title }}</h1>
                        <Message class="w-full mb-4" v-if="!ecoSpaceIsFinished(selectedSpace)">
                            <template #icon>
                                <div class="flex items-center">
                                    <Icon name="ic:baseline-info" class="size-5" />
                                </div>
                            </template>
                            <span class="text-md">
                                Dieser EcoSpace wurde noch nicht beendet. Wenn Sie diesen EcoSpace fortsetzen wollen,
                                können Sie <Button variant="link" class="!p-0">
                                    <template #default>
                                        <span
                                            class="text-blue-700 hover:text-blue-500 hover:underline font-bold">hier</span>
                                    </template>
                                </Button>
                                klicken um diesem beizutreten.
                            </span>
                        </Message>
                        <Panel header="Informationen" class="w-full mb-4">
                            <Divider />
                            <div class="w-full h-full flex justify-between">
                                <div class="flex flex-col justify-between flex-1 mr-40">
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
                                            <span class="font-bold">Zielgruppe:</span>
                                            TODO
                                        </p>
                                    </div>
                                    <div class="w-full">
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
                                </div>
                                <Fieldset legend="Teilnehmer" class="max-h-64 overflow-scroll">
                                    <div class="h-full w-full">
                                        <DataTable :value="selectedSpace.participants">
                                            <Column field="userName" header="Username">
                                                <template #body="{ data }">
                                                    <div class="flex items-center">
                                                        <div class="size-5 mr-1">
                                                            <Icon name="ic:baseline-star-rate"
                                                                class="size-5 bg-yellow-500"
                                                                v-tooltip.bottom="{ value: 'Host', showDelay: 50 }"
                                                                v-if="data.isHost" />
                                                        </div>
                                                        <span>{{ data.userName }}</span>
                                                    </div>
                                                </template>
                                            </Column>
                                            <Column field="impact" header="Impact">
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
                        <h2 class="text-2xl mb-2">Storyteile</h2>
                        <Accordion :value="Array.from(Array(selectedSpace.story.parts.length).keys())" multiple
                            class="w-full my-2">
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
                                                        <p>{{ choice.text }}</p>
                                                        <Badge class="!bg-white !text-black"
                                                            :value="choice.numberVotes.toString() + ' Stimmen'" />
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

const runtimeConfig = useRuntimeConfig();
const confirmDialog = useConfirm();
const toast = useToast();

const route = useRoute();


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


const showFilters = ref(false);

const filters: OverviewFilter = {
    applied: {
        finished: ref('Alle'),
        date: ref<Date | Date[] | (Date | null)[] | null | undefined>(undefined),
        sort: {
            subject: ref('Erstellungsdatum'),
            direction: ref(false) // false = ascending, true = descending
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

const spaceRefsById = spaces.value ? spaces.value.reduce((acc, space) => {
    acc[space.id] = ref(false);
    return acc;
}, {} as Record<string, Ref<boolean>>) : {};

const selectedSpace = ref<EcoSpace>();

const searchInput = ref('');

const searchedSpaces = computed(() => {
    return sortedSpaces.value.filter(space => {
        if (space.story.title) {
            return space.story.title.toLowerCase().includes(searchInput.value.toLowerCase())
        } else {
            if (searchInput.value !== "") {
                return false
            }
            return true
        }
    });
});

const filteredSpaces = computed<EcoSpace[]>(() => {
    if (!spaces.value) return [];

    const normalizeDate = (date: Date) => {
        return new Date(date.getFullYear(), date.getMonth(), date.getDate());
    }
    const result = spaces.value?.filter(space => {

        let finished;
        switch (filters.applied.finished.value) {
            case 'Alle':
                finished = true;
                break;
            case 'Beendet':
                finished = ecoSpaceIsFinished(space)
                break;
            case 'Nicht beendet':
                finished = !ecoSpaceIsFinished(space)
                break;
        }

        if (filters.applied.date.value) {
            if (Array.isArray(filters.applied.date.value)) {
                if (filters.applied.date.value[0] !== null) {
                    let fromDate = normalizeDate(new Date(space.createdAt)) >= filters.applied.date.value[0];
                    if (filters.applied.date.value[1] !== null) {
                        let toDate = normalizeDate(new Date(space.createdAt)) <= filters.applied.date.value[1];
                        return finished && fromDate && toDate;
                    } else {
                        return finished && fromDate;
                    }
                }
            } else {
                return finished;
            }
        } else {
            return finished;
        }
    });
    return Array.isArray(result) ? result : [result];
});

const sortedSpaces = computed<EcoSpace[]>(() => {
    const sortedList = filteredSpaces.value.sort((a, b) => {
        switch (filters.applied.sort.subject.value) {
            case 'Erstellungsdatum':
                return filters.applied.sort.direction.value ? new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime() : new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
            case 'Titel':
                return filters.applied.sort.direction.value ? b.story.title.localeCompare(a.story.title) : a.story.title.localeCompare(b.story.title);
            case 'Anzahl der Entscheidungspunkte':
                return filters.applied.sort.direction.value ? a.story.length - b.story.length : b.story.length - a.story.length;
            case 'Anzahl der Teilnehmer':
                return filters.applied.sort.direction.value ? a.participants.length - b.participants.length : b.participants.length - a.participants.length;
            default:
                return 0;
        }
    });
    return sortedList;
})

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
    let percentage = (selectedSpace.value!.story.parts.length / selectedSpace.value!.story.length) * 100;
    return [{ label: '', value: percentage, color: 'var(--p-primary-color)' }];
}

function getProgressLabel() {
    let relative = selectedSpace.value!.story.parts.length / selectedSpace.value!.story.length;
    if (relative < 1) {
        return `${selectedSpace.value!.story.parts.length} von ${selectedSpace.value!.story.length} Entscheidungspunkten wurden abgeschlossen`;
    }
    return `Alle ${selectedSpace.value!.story.length} Entscheidungspunkten wurden abgeschlossen`;
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
</script>

<style scoped></style>