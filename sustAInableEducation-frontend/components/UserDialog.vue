<template>
    <Dialog v-model:visible="model" modal header="Teilnehmer" class="sm:w-96">
        <div class="flex flex-col items-center">
            <IconField class="mb-4 w-full">
                <InputIcon>
                    <Icon name="ic:baseline-search"/>
                </InputIcon>
                <InputText placeholder="Suchen" v-model="searchInput" class="w-full" />
            </IconField>
            <Panel class="overflow-y-scroll h-96 w-full">
                <DataTable :value="searchedParticipants">
                    <Column field="userName" header="Name" class="w-8/12">
                        <template #body="{ data }">
                            <div class="flex items-center">
                                <div class="size-5 mr-1">
                                    <Icon name="ic:baseline-star-rate" class="size-5 bg-yellow-500"
                                        v-tooltip.bottom="{ value: 'Host', showDelay: 50 }" v-if="data.isHost" />
                                </div>
                                <span>{{ data.userName }}</span>
                            </div>
                        </template>
                    </Column>
                    <Column field="isOnline" header="Status" class="w-4/12">
                        <template #body="{ data }">
                            <div class="flex items-center">
                                <span>{{ data.isOnline ? 'Online' : 'Offline' }}</span>
                            </div>
                        </template>
                    </Column>
                </DataTable>
            </Panel>
        </div>
    </Dialog>
</template>

<script setup lang="ts">
import type { Participant } from '~/types/EcoSpace';

const props = defineProps<{ participants: Participant[] }>();

const model = defineModel<boolean>();

const searchInput = ref('');

const searchedParticipants = computed(() => {
    return props.participants.filter((participant: Participant) => {
        return participant.userName.toLowerCase().includes(searchInput.value.toLowerCase());
    });
})


</script>