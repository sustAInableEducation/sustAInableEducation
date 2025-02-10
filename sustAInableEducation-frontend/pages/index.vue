<template>
    <div class="w-full h-full">
        <div class="background animate-anim" />
        <Toast />
        <div class="w-screen flex justify-center items-center h-full">
            <div class="bg-slate-50 shadow-xl flex justify-between flex-col rounded-xl items-center p-8 mx-4">
                <h1 class="text-3xl font-bold mb-4">EcoSpace beitreten</h1>
                <p class="mb-4">
                    Bitte gib den 6-stelligen Code ein, um dem Space beizutreten.
                </p>
                <form @submit.prevent class="flex justify-center flex-col items-center">
                    <InputOtp :length="6" v-model="value" integerOnly class="mb-4" />
                    <Button class="w-3/6" type="submit" :disabled="!codeFilledOut"
                        @click="joinEcoSpace(value)" :loading="loading">Beitreten</Button>
                </form>

                <p class="mt-4">oder</p>
                <NuxtLink to="/configuration">
                    <Button variant="link" label="EcoSpace erstellen" />
                </NuxtLink>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">

useHead({
    title: 'sustAInableEducation'
})

const runtimeConfig = useRuntimeConfig()
const toast = useToast()

const value = ref('')

const loading = ref(false)

onMounted(() => {
    isLoggedInRequest()
})

const codeFilledOut = computed(() => {
    return value.value.length === 6
})

function isLoggedInRequest() {
    $fetch(`${runtimeConfig.public.apiUrl}/account`, {
        method: 'GET',
        credentials: 'include',
        onResponse: (response) => {
            if (response.response.status === 401) {
                navigateTo('/login')
            }
        }
    })
}

function joinEcoSpace(code: string) {
    loading.value = true;

    $fetch(`${runtimeConfig.public.apiUrl}/spaces/join`, {
        method: 'POST',
        credentials: 'include',
        body: {
            code: code
        },
        onResponse: (response) => {
            if (response.response.ok) {
                navigateTo('/spaces/' + response.response._data.id)
            } else {
                loading.value = false;
                console.log(loading.value)
                toast.add({
                    severity: 'error',
                    summary: 'Fehler',
                    detail: 'Der Code ist ungültig.',
                    life: 5000
                })
            }
        }
    })
}



</script>