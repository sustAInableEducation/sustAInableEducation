<template>
    <div class="w-full h-full">
        <div class="background animate-anim" />
        <div class="w-screen flex justify-center items-center h-full ">
            <Toast />
            <div
                class="bg-slate-50 shadow-xl flex justify-between flex-col rounded-xl items-center p-8 w-full max-w-md mx-4">
                <h1 class="text-3xl font-bold mb-4">Registrierung</h1>
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
                        <Message v-if="$form.password?.invalid" severity="error" size="small" class="mt-2">
                            {{ $form.password.error?.message }}
                        </Message>
                    </div>
                    <div>
                        <FloatLabel variant="in" class="flex flex-col">
                            <Password toggleMask :feedback="false" name="confirmPassword" type="password" fluid />
                            <label for="confirmPassword">Passwort Bestätigen</label>
                        </FloatLabel>
                        <Message v-if="$form.confirmPassword?.invalid" severity="error" size="small" class="mt-2">{{
                            $form.confirmPassword.error?.message }}</Message>
                    </div>
                    <Button type="submit" label="Registrieren" :loading="loading" />
                </Form>
                <p class="mt-4">oder</p>
                <NuxtLink :to="redirection ? '/login?redirect=' + redirection : 'login'"
                    class="text-white mx-4 text-xl">
                    <Button variant="link" label="Anmelden" text />
                </NuxtLink>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import type { Register, RegisterError } from '~/types/register'

useHead({
    title: 'Registrierung - sustAInableEducation'
})

const runtimeConfig = useRuntimeConfig();

const route = useRoute();
const router = useRouter();

const redirection = route.query.redirect as string | undefined;

const cookieHeaders = useRequestHeaders(['cookie']);

await isLoggedInRequest();

const toast = useToast();

const formRefs = {
    email: ref<string>(''),
    password: ref<string>('')
}

const loading = ref(false)

const initialValues = reactive({
    email: '',
    password: '',
    confirmPassword: ''
});

const resolver = ({ values }: { values: Register }) => {
    const errors: RegisterError = {
        email: [],
        password: [],
        confirmPassword: []
    };

    if (!values.email) {
        errors.email.push({ message: 'Bitte geben Sie eine Email ein.' });
    } else if (!values.email.match(/(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|"(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9]))\.){3}(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9])|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])/)) {
        errors.email.push({ message: 'Die eingegebene Email ist nicht valide.' });
    } else {
        errors.email = [];
    }

    let passwordValid = true;
    if (!values.password) {
        errors.password.push({ message: 'Bitte geben Sie ein Passwort ein.' });
        passwordValid = false;
    } else {
        if (values.password.length < 8) {
            errors.password.push({ message: 'Das Passwort muss mindestens 8 Zeichen lang sein.' });
            passwordValid = false;
        }
        if (includesSpecialCharacter(values.password)) {
            errors.password.push({ message: 'Das Passwort muss mindestens ein Sonderzeichen enthalten.' });
            passwordValid = false;
        }
        if (includesNumber(values.password)) {
            errors.password.push({ message: 'Das Passwort muss mindestens eine Ziffer enthalten.' });
            passwordValid = false;
        }
        if (includesUppercase(values.password)) {
            errors.password.push({ message: 'Das Passwort muss mindestens einen Großbuchstaben enthalten.' });
            passwordValid = false;
        }
        if (includesLowercase(values.password)) {
            errors.password.push({ message: 'Das Passwort muss mindestens einen Kleinbuchstaben enthalten.' });
            passwordValid = false;
        }
    }

    if (passwordValid) {
        errors.password = [];
    }

    if (values.password !== values.confirmPassword) {
        errors.confirmPassword.push({ message: 'Die Passwörter stimmen nicht überein.' });
    } else {
        errors.confirmPassword = [];
    }


    return {
        errors
    };
};

const onFormSubmit = ({ valid }: { valid: boolean }) => {
    if (valid) {
        register();
    }
}

async function register() {
    loading.value = true;
    await $fetch(`${runtimeConfig.public.apiUrl}/account/register`, {
        method: 'POST',
        body: JSON.stringify({
            "email": formRefs.email.value,
            "password": formRefs.password.value
        }),
        onResponse: (response) => {
            if (response.response.ok) {
                if (redirection) {
                    navigateTo('/login?redirect=' + redirection);
                } else {
                    navigateTo('/login');
                }
            }
        },
        onRequestError: (error) => {
            toast.add({ severity: 'error', summary: `Fehler: ${error.response?.status}`, detail: 'Bei der Registrierung ist ein Fehler aufgetreten.' });
            loading.value = false;
        }
    })
}

function includesSpecialCharacter(str: string) {
    let RegEx = /^[a-z0-9]+$/i;
    return RegEx.test(str);
}

function includesNumber(str: string) {
    let RegEx = /\d/;
    return !RegEx.test(str);
}

function includesUppercase(str: string) {
    let RegEx = /[A-ZÄÖÜ]/;
    return !RegEx.test(str);
}

function includesLowercase(str: string) {
    let RegEx = /[a-zäöü]/;
    return !RegEx.test(str);
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