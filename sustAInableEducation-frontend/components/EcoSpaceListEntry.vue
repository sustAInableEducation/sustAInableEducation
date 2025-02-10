<template>
    <div class=" mb-2 p-2 rounded-md cursor-pointer" :class="model ? 'bg-slate-300' : 'bg-slate-200'" @click="clickEvent">
        <div class="flex justify-between items-center">
            <span>{{ displayTitle() }}</span>
            <Button v-if="showDelete" variant="text" class="!rounded-full aspect-square !p-1" @click="toggleMenu">
                <Icon name="ic:baseline-more-vert" class="aspect-square size-6 bg-black" />
            </Button>
            <Menu ref="menu" :model="menuItems" :popup="true" class="!rounded-md">
                <template #item="{ item, props }">
                    <a v-ripple class="flex items-center" v-bind="props.action">
                        <Icon name="ic:baseline-delete" class="mr-2 size-5" />
                        <span>{{ item.label }}</span>
                        <Badge v-if="item.badge" class="ml-auto" :value="item.badge" />
                        <span v-if="item.shortcut"
                            class="ml-auto border border-surface rounded bg-emphasis text-muted-color text-xs p-1">{{
                            item.shortcut }}</span>
                    </a>
                </template>
            </Menu>
        </div>
        <span> {{ formatDate(props.ecoSpace.createdAt) }}</span>
    </div>
</template>

<script setup lang="ts">
import type { EcoSpace } from '~/types/EcoSpace';

const model = defineModel<boolean>();

const emit = defineEmits(['click', 'delete']);

const props = defineProps<{ ecoSpace: EcoSpace, showDelete: boolean }>();

const menu = ref();

const menuItems = ref([
    {
        label: 'Löschen',
        Icon: 'ic:baseline-delete',
        command: () => deleteEvent()
    }
]);

function formatDate(dateString: string) {
    const date = new Date(dateString);
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    return `Erstellt am ${day}.${month}.${year}`;
}

const toggleMenu = (event: Event) => {
    menu.value.toggle(event);
};

function clickEvent() {
    emit('click', model.value);
}

function deleteEvent() {
    emit('delete', props.ecoSpace.id);
}

function displayTitle() {
    if(props.ecoSpace.story.title) {
        return props.ecoSpace.story.title
    } else {
        return "[Neuer EcoSpace]"
    }
}

</script>