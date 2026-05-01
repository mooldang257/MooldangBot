<script lang="ts">
    import QueueInlineWidget from './QueueInlineWidget.svelte';
    import QueueCardWidget from './QueueCardWidget.svelte';

    const { queue = [], settings = {}, layout = {} } = $props<{ 
        queue: any[], 
        settings: any,
        layout: any
    }>();
</script>

{#if settings.queueTheme === 'inline'}
    <QueueInlineWidget {queue} {settings} {layout} />
{:else}
    <QueueCardWidget {queue} {settings} {layout} />
{/if}
