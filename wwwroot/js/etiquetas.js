// ── Imprimir etiquetas de alumnos ──────────────────────────────────────────
window.imprimirEtiquetas = function(
    nombres, titulo, textoAdicional,
    anchoMm, altoMm, columnas,
    tamTitulo, tamNombre,
    imgIzqData, imgFondoData, opacidadFondo
) {
    const pxPerMm    = 3.7795275591;
    const anchoLabel = anchoMm * pxPerMm;
    const altoLabel  = altoMm  * pxPerMm;
    const gap        = 3 * pxPerMm;
    const padH       = Math.round(altoLabel  * 0.07);
    const padW       = Math.round(anchoLabel * 0.04);
    const gapInner   = Math.round(anchoLabel * 0.03);

    const estiloLabel = [
        `width:${anchoLabel}px`,
        `height:${altoLabel}px`,
        `display:inline-flex`,
        `align-items:center`,
        `position:relative`,
        `overflow:hidden`,
        `border:1px solid #ccc`,
        `border-radius:4px`,
        `background:white`,
        `box-sizing:border-box`,
        `padding:${padH}px ${padW}px`,
        `gap:${gapInner}px`,
        `page-break-inside:avoid`,
        `vertical-align:top`,
        `margin:${gap / 2}px`
    ].join(';');

    const etiquetasHTML = nombres.map(nombre => {

        // Fondo: usar <img> con posición absoluta en lugar de background-image
        // Los navegadores siempre imprimen <img> aunque tengan desactivada
        // la opción "Imprimir imágenes de fondo"
        const fondoHTML = imgFondoData
            ? `<img src="${imgFondoData}"
                    style="position:absolute;inset:0;width:100%;height:100%;
                           object-fit:cover;object-position:center;
                           opacity:${opacidadFondo / 100};
                           filter:blur(3px);
                           pointer-events:none;"
                    aria-hidden="true" />`
            : '';

        const imgIzqHTML = imgIzqData
            ? `<img src="${imgIzqData}"
                    style="height:72%;object-fit:contain;
                           flex-shrink:0;max-width:28%;
                           position:relative;z-index:1;" />`
            : '';

        const textoHTML = `
            <div style="flex:1;min-width:0;position:relative;z-index:1;overflow:hidden;">
                ${titulo
                    ? `<div style="font-size:${tamTitulo}px;font-weight:700;
                           line-height:1.15;color:#1a1a2e;margin-bottom:1px;
                           white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">
                           ${titulo}</div>`
                    : ''}
                ${nombre
                    ? `<div style="font-size:${tamNombre}px;font-weight:600;
                           color:#e63946;line-height:1.2;
                           white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">
                           ${nombre}</div>`
                    : ''}
                ${textoAdicional
                    ? `<div style="font-size:${Math.max(7, tamNombre - 2)}px;
                           color:#555;margin-top:1px;line-height:1.2;
                           white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">
                           ${textoAdicional}</div>`
                    : ''}
            </div>`;

        return `<div style="${estiloLabel}">${fondoHTML}${imgIzqHTML}${textoHTML}</div>`;
    }).join('');

    const html = `<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="utf-8"/>
    <title>Etiquetas · AMPA</title>
    <style>
        * { box-sizing:border-box; margin:0; padding:0; }

        body {
            font-family: Arial, Helvetica, sans-serif;
            background: #eee;
            padding: 8mm;
        }

        .info {
            background: white;
            padding: 3mm 5mm;
            border-radius: 4px;
            font-size: 11px;
            color: #666;
            margin-bottom: 6mm;
        }

        .grid {
            display: flex;
            flex-wrap: wrap;
            align-items: flex-start;
        }

        @media print {
            body {
                background: white;
                padding: 3mm;
                /* Forzar al navegador a imprimir colores e imágenes de fondo */
                -webkit-print-color-adjust: exact;
                print-color-adjust: exact;
                color-adjust: exact;
            }
            .info { display: none; }
            @page {
                margin: 5mm;
                size: A4;
            }
            /* Las etiquetas con img absoluta se imprimen siempre */
            img {
                -webkit-print-color-adjust: exact;
                print-color-adjust: exact;
            }
        }
    </style>
</head>
<body>
    <div class="info">
        <b>Etiquetas AMPA</b> &nbsp;·&nbsp;
        ${nombres.length} etiqueta${nombres.length !== 1 ? 's' : ''} &nbsp;·&nbsp;
        ${anchoMm}×${altoMm} mm &nbsp;·&nbsp;
        ${columnas} columna${columnas > 1 ? 's' : ''} &nbsp;·&nbsp;
        Imprime en A4 y recorta por el borde
    </div>
    <div class="grid">
        ${etiquetasHTML}
    </div>
    <script>
        // Esperar a que las imágenes base64 carguen antes de abrir el diálogo
        window.addEventListener('load', function () {
            var imgs = document.querySelectorAll('img');
            var total = imgs.length;
            if (total === 0) { setTimeout(function(){ window.print(); }, 300); return; }
            var cargadas = 0;
            function intentarImprimir() {
                cargadas++;
                if (cargadas >= total) setTimeout(function(){ window.print(); }, 300);
            }
            imgs.forEach(function(img) {
                if (img.complete) { intentarImprimir(); }
                else { img.addEventListener('load', intentarImprimir); img.addEventListener('error', intentarImprimir); }
            });
        });
    <\/script>
</body>
</html>`;

    const w = window.open('', '_blank');
    if (!w) { alert('Permite las ventanas emergentes para imprimir.'); return; }
    w.document.write(html);
    w.document.close();
};
