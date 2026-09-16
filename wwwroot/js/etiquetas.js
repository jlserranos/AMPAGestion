// ── Imprimir etiquetas de alumnos ──────────────────────────────────────────
window.imprimirEtiquetas = function(
    nombres, titulo, textoAdicional,
    anchoMm, altoMm, columnas,
    tamTitulo, tamNombre,
    imgIzqData, imgFondoData, opacidadFondo
) {
    const pxPerMm = 3.7795275591;
    const anchoLabel = anchoMm * pxPerMm;
    const altoLabel  = altoMm  * pxPerMm;
    const gap        = 3 * pxPerMm;

    const estiloEtiqueta = [
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
        `padding:${Math.round(altoLabel * 0.07)}px ${Math.round(anchoLabel * 0.04)}px`,
        `gap:${Math.round(anchoLabel * 0.03)}px`,
        `page-break-inside:avoid`,
        `vertical-align:top`,
        `margin:${gap/2}px`
    ].join(';');

    const etiquetasHTML = nombres.map(nombre => {
        const fondo = imgFondoData
            ? `<div style="position:absolute;inset:0;background-image:url('${imgFondoData}');background-size:cover;background-position:center;opacity:${opacidadFondo/100};filter:blur(3px);"></div>`
            : '';

        const imgIzq = imgIzqData
            ? `<img src="${imgIzqData}" style="height:72%;object-fit:contain;flex-shrink:0;max-width:28%;position:relative;z-index:1;" />`
            : '';

        const texto = [
            titulo ? `<div style="font-size:${tamTitulo}px;font-weight:700;line-height:1.15;color:#1a1a2e;margin-bottom:1px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">${titulo}</div>` : '',
            nombre ? `<div style="font-size:${tamNombre}px;font-weight:600;color:#e63946;line-height:1.2;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">${nombre}</div>` : '',
            textoAdicional ? `<div style="font-size:${Math.max(7, tamNombre - 2)}px;color:#555;margin-top:1px;line-height:1.2;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">${textoAdicional}</div>` : ''
        ].filter(Boolean).join('');

        return `<div style="${estiloEtiqueta}">${fondo}${imgIzq}<div style="flex:1;min-width:0;position:relative;z-index:1;overflow:hidden;">${texto}</div></div>`;
    }).join('');

    const html = `<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="utf-8"/>
    <title>Etiquetas · AMPA</title>
    <style>
        *    { box-sizing:border-box; margin:0; padding:0; }
        body { font-family:Arial,Helvetica,sans-serif; background:#eee; padding:8mm; }
        .info { background:white; padding:3mm 5mm; border-radius:4px; font-size:11px; color:#666; margin-bottom:6mm; }
        .grid { display:flex; flex-wrap:wrap; align-items:flex-start; }
        @media print {
            body { background:white; padding:3mm; }
            .info { display:none; }
            @page { margin:5mm; size:A4; }
        }
    </style>
</head>
<body>
    <div class="info">
        <b>Etiquetas AMPA</b> · ${nombres.length} etiquetas · ${anchoMm}×${altoMm} mm · ${columnas} col. · Recorta por el borde
    </div>
    <div class="grid">${etiquetasHTML}</div>
    <script>window.addEventListener('load',()=>setTimeout(()=>window.print(),600));<\/script>
</body>
</html>`;

    const w = window.open('', '_blank');
    w.document.write(html);
    w.document.close();
};
