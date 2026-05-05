<script lang="ts">
    import type { WidgetConfig, LayoutConfig } from './registry';
    import { normalizeLayout } from './registry';

    let { widget, settings = {}, layout: rawLayout = {}, ...rest } = $props<{
        widget: WidgetConfig;
        settings: any;
        layout: any;
        [key: string]: any;
    }>();

    let layout = $derived(normalizeLayout(rawLayout, widget.DefaultLayout));
</script>

{#if layout.Visible}
    <div 
        class="widget-container" 
        style="
            left: {layout.X}px; 
            top: {layout.Y}px; 
            width: {layout.Width}px; 
            height: {layout.Height}px; 
            opacity: {layout.Opacity};
        "
    >
        <widget.Component {settings} layout={layout} {...rest} />
    </div>
{/if}

<style>
    .widget-container {
        position: absolute;
        pointer-events: none;
    }
</style>
