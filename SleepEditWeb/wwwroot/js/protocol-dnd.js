window.protocolDnd = {
    _instances: {},

    init(dotNetRef, listElementIds) {
        const self = this;
        listElementIds.forEach(id => {
            const el = document.getElementById(id);
            if (!el) return;
            self._instances[id]?.destroy();
            self._instances[id] = Sortable.create(el, {
                group: 'protocol-nodes',
                animation: 150,
                ghostClass: 'protocol-drag-ghost',
                onEnd(evt) {
                    const nodeId = parseInt(evt.item.dataset.nodeId, 10);
                    const parentId = parseInt(evt.to.dataset.parentId, 10);
                    const newIndex = evt.newIndex;
                    dotNetRef.invokeMethodAsync('OnDrop', nodeId, parentId, newIndex);
                }
            });
        });
    },

    destroy(listElementIds) {
        const self = this;
        listElementIds.forEach(id => {
            self._instances[id]?.destroy();
            delete self._instances[id];
        });
    }
};
