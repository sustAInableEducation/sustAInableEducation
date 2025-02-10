<template>
    <Dialog v-model:visible="model" modal header="Teilnehmer" class="w-96 sm:w-[32rem]" :draggable="false">
        <div class="flex flex-col items-center">
            <IconField class="mb-4 w-full">
                <InputIcon>
                    <Icon name="ic:baseline-search"/>
                </InputIcon>
                <InputText placeholder="Suchen" v-model="searchInput" class="w-full" />
            </IconField>
            <Panel class="overflow-y-scroll h-96 w-full">
                <DataTable :value="searchedParticipants">
                    <Column field="userName" header="Benutzername" class="w-8/12">
                        <template #body="{ data }">
                            <div class="flex items-center">
                                <div class="size-5 mr-1">
                                    <Icon name="ic:baseline-star-rate" class="size-5 bg-yellow-500"
                                        v-tooltip.bottom="{ value: 'Host', showDelay: 50 }" v-if="data.isHost" />
                                </div>
                                <span :class="[data.userId === props.myUserId ? 'font-bold underline decoration-2' : '']">{{ data.userName }}</span>
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

const props = defineProps<{ participants: Participant[], myUserId: string }>();

const model = defineModel<boolean>();

const searchInput = ref('');

const searchedParticipants = computed(() => {
    return props.participants.filter((participant: Participant) => {
        return participant.userName.toLowerCase().includes(searchInput.value.toLowerCase());
    });
})


</script>