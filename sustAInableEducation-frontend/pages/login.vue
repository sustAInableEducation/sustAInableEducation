<template>
    <div class="w-full h-full">
        <div class="background animate-anim" />
        <div class="w-screen flex justify-center items-center h-full ">
            <Toast />
            <div
                class="bg-slate-50 shadow-xl flex justify-between flex-col rounded-xl items-center p-8 w-full max-w-md mx-4">
                <h1 class="text-3xl font-bold mb-4">Anmeldung</h1>
                <Form v-slot="$form" :initialValues :resolver @submit="onFormSubmit"
                    class="flex flex-col gap-4 !w-full sm:w-56">
                    <div>
                        <FloatLabel variant="in" class="flex flex-col">
                            <InputText v-model="formRefs.email.value" name="email" type="email" fluid />
                            <label for="email">E-Mail</label>
                        </FloatLabel>
                        <Message v-if="$form.email?.invalid" severity="error" size="small" class="mt-2">{{
                            $form.email.error?.message }}</Message>
                    </div>
                    <div>
                        <FloatLabel variant="in" class="flex flex-col">
                            <Password toggleMask :feedback="false" v-model="formRefs.password.value" name="password"
                                type="password" fluid />
                            <label for="password">Passwort</label>
                        </FloatLabel>
                        <Message v-if="$form.password?.invalid" severity="error" size="small" class="mt-2">{{
                            $form.password.error?.message }}</Message>
                    </div>
                    <div class="flex items-center ml-1">
                        <Checkbox name="saveLogin" v-model="formRefs.saveLogin.value" binary value="saveLogin"
                            inputId="saveLogin" />
                        <label for="saveLogin" class="ml-2 cursor-pointer">Eingeloggt bleiben</label>
                    </div>
                    <Button type="submit" label="Anmelden" :loading="loading" />
                </Form>
                <p class="mt-4">oder</p>
                <NuxtLink :to="redirection ? '/register?redirect=' + redirection : '/register'"
                    class="text-white mx-4 text-xl">
                    <Button variant="link" label="Registrieren" text />
                </NuxtLink>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import type { Login, LoginError } from '~/types/login'

useHead({
    title: 'Anmeldung - sustAInableEducation'
})

const runtimeConfig = useRuntimeConfig();

const route = useRoute();
const router = useRouter();

const toast = useToast();

const redirection = route.query.redirect as string | undefined;

const cookieHeaders = useRequestHeaders(['cookie']);

await isLoggedInRequest();

const formRefs = {
    email: ref<string>(''),
    password: ref<string>(''),
    saveLogin: ref<boolean>(false)
}

const loading = ref(false)

const initialValues = reactive({
    email: '',
    password: '',
});

const resolver = ({ values }: { values: Login }) => {
    const errors: LoginError = {
        email: [],
        password: []
    };

    if (!values.email) {
        errors.email = [{ message: 'Bitte geben Sie eine E-Mail ein.' }];
    }

    if (!values.password) {
        errors.password = [{ message: 'Bitte geben Sie ein Passwort ein.' }];
    }

    return {
        errors
    };
};

const onFormSubmit = ({ valid }: { valid: boolean }) => {
    if (valid) {
        login();
    }
};

async function login() {
    loading.value = true;
    let url = `${runtimeConfig.public.apiUrl}/account/login`;
    if (formRefs.saveLogin.value) {
        url += "?useCookies=true";
    } else {
        url += "?useSessionCookies=true";
    }

    try {
        await $fetch(url, {
            method: 'POST',
            body: JSON.stringify({
                "email": formRefs.email.value,
                "password": formRefs.password.value
            }),
            credentials: 'include',
            onResponse: (response) => {
                if (response.response.status === 200) {
                    if (redirection) {
                        navigateTo(redirection);
                    } else {
                        navigateTo('/');
                    }
                } else {
                    loading.value = false;
                    toast.add({ severity: 'error', summary: `Fehler: ${response.response.status}`, detail: 'Bei der Anmeldung ist ein Fehler aufgetreten.' });
                }
            }
        })
    } catch (e) { }
}

async function isLoggedInRequest() {
    try {
        await $fetch(`${runtimeConfig.public.apiUrl}/account`, {
            method: 'GET',
            credentials: 'include',
            headers: cookieHeaders,
            onResponse: (response) => {
                if (response.response.status === 200) {
                    router.push('/');
                }
            }
        })
    } catch (e) { }
}
</script>