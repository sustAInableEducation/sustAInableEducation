<template>
    <div class="flex flex-col h-screen">
        <header class="absolute z-20">
            <Header />
        </header>
        <main class="h-full">
            <slot />
        </main>
        <footer>

        </footer>
    </div>
</template>

<script setup>

</script>

<style lang="scss" scoped>

</style>