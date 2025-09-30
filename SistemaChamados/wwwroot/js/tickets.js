// tickets.js — preview e validações no cliente (Create modal)
(function () {
    const fmtSize = (bytes) => {
        if (bytes < 1024) return `${bytes} B`;
        const kb = bytes / 1024;
        if (kb < 1024) return `${kb.toFixed(1)} KB`;
        return `${(kb / 1024).toFixed(2)} MB`;
    };

    const input = document.getElementById("fileInput");
    const preview = document.getElementById("filePreview");
    const btnAnexar = document.getElementById("btnAnexar");

    if (btnAnexar && input) {
        btnAnexar.addEventListener("click", () => input.click());
    }

    if (input && preview) {
        const maxFiles = parseInt(input.dataset.maxFiles || "10", 10);
        const maxSizeMb = parseInt(input.dataset.maxSizeMb || "10", 10);

        input.addEventListener("change", function () {
            preview.innerHTML = "";
            const files = Array.from(input.files || []);

            if (files.length > maxFiles) {
                alert(`Selecione no máximo ${maxFiles} arquivo(s).`);
                input.value = "";
                return;
            }

            const overs = files.filter(f => (f.size / (1024 * 1024)) > maxSizeMb);
            if (overs.length > 0) {
                const names = overs.map(f => f.name).join(", ");
                alert(`Arquivo(s) acima de ${maxSizeMb} MB: ${names}`);
                input.value = "";
                return;
            }

            files.forEach(f => {
                const row = document.createElement("div");
                row.className = "file-item";
                row.innerHTML = `<span>📎</span><span class="name">${f.name}</span><span class="size">${fmtSize(f.size)}</span>`;
                preview.appendChild(row);
            });
        });
    }

    // jQuery Validate (reforço)
    $(function () {
        $("#ticketForm").validate({
            errorClass: "text-danger",
            rules: {
                "Input.Titulo": { required: true, maxlength: 120 },
                "Input.Descricao": { required: true, maxlength: 2000 },
                "Input.CategoriaId": { required: true }
            },
            messages: {
                "Input.Titulo": { required: "Informe um título." },
                "Input.Descricao": { required: "Descreva o problema." },
                "Input.CategoriaId": { required: "Selecione a categoria." }
            }
        });
    });

    // TODO: quando existir backend, enviar via POST /api/tickets e /api/tickets/{id}/attachments
})();
