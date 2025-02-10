<script lang="ts" setup>
const router = useRouter()
const route = useRoute()
const code = route.params.code

const requestHeaders = useRequestHeaders(['cookie'])

const runtimeConfig = useRuntimeConfig()
console.log(code)
if (import.meta.client) {
  await $fetch(`${runtimeConfig.public.apiUrl}/spaces/join`, {
    method: 'POST',
    credentials: 'include',
    body: {
      code: code
    },
    headers: requestHeaders,
    onRequestError: (error) => {
      if (error) {
        console.error('Failed to join')
      }
    },
    onResponse: (response) => {
      if (response.response.ok) {
        router.push(`/spaces/${response.response._data.id}`)
      } else if (response.response.status === 404) {
        router.push('/')
      } else if (response.response.status === 401) {
        router.push('/login?redirect=' + route.fullPath)
      } else {
      }
    }
  })
}
</script>

<style></style>