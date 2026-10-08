// ── Imprimir etiquetas de alumnos ──────────────────────────────────────────
// Recibe un único objeto de configuración desde Etiquetas.razor (camelCase).
// Genera páginas A4 explícitas: cada etiqueta queda dentro de los márgenes
// configurados (área imprimible) y el pie se coloca en el margen inferior
// izquierdo, también dentro de los márgenes.
window.imprimirEtiquetas = function (cfg) {

    const esc = s => String(s ?? '')
        .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');

    const nombres   = cfg.nombres || [];
    const anchoMm   = cfg.anchoMm;
    const altoMm    = cfg.altoMm;
    const columnas  = Math.max(1, cfg.columnas);
    const filas     = Math.max(1, cfg.filas);
    const sep       = cfg.separacionMm;          // espacio entre etiquetas
    const espTexto  = cfg.espaciadoTextoMm;      // espacio entre título / nombre / adicional
    const m         = cfg.margenes;              // { superior, inferior, izquierdo, derecho }
    const reservaPie = cfg.reservaPieMm || 0;    // alto reservado para el pie

    const estiloTexto = (e, extra) =>
        `font-size:${e.tamano}px;font-family:${e.fuente};color:${e.color};` +
        `font-weight:${e.negrita ? 700 : 400};line-height:1.15;` +
        `white-space:nowrap;overflow:hidden;text-overflow:ellipsis;${extra || ''}`;

    const padV = (altoMm  * 0.07).toFixed(2);
    const padH = (anchoMm * 0.04).toFixed(2);
    const gapInner = (anchoMm * 0.03).toFixed(2);

    const fondoHTML = cfg.imgFondoData
        ? `<img src="${cfg.imgFondoData}" aria-hidden="true"
                style="position:absolute;inset:0;width:100%;height:100%;
                       object-fit:cover;object-position:center;
                       opacity:${cfg.opacidadFondo / 100};filter:blur(3px);
                       pointer-events:none;" />`
        : '';

    const imgIzqHTML = cfg.imgIzqData
        ? `<img src="${cfg.imgIzqData}"
                style="height:72%;object-fit:contain;flex-shrink:0;max-width:28%;
                       position:relative;z-index:1;" />`
        : '';

    const etiqueta = nombre => {
        const lineas = [];
        if (cfg.titulo)         lineas.push(`<div style="${estiloTexto(cfg.estiloTitulo)}">${esc(cfg.titulo)}</div>`);
        if (nombre)             lineas.push(`<div style="${estiloTexto(cfg.estiloNombre)}">${esc(nombre)}</div>`);
        if (cfg.textoAdicional) lineas.push(`<div style="${estiloTexto(cfg.estiloAdicional)}">${esc(cfg.textoAdicional)}</div>`);

        return `<div class="etq" style="padding:${padV}mm ${padH}mm;gap:${gapInner}mm">
                    ${fondoHTML}${imgIzqHTML}
                    <div class="txt" style="gap:${espTexto}mm">${lineas.join('')}</div>
                </div>`;
    };

    const porPagina = columnas * filas;
    const paginas = [];
    for (let i = 0; i < nombres.length; i += porPagina)
        paginas.push(nombres.slice(i, i + porPagina));

    const pieHTML = cfg.mostrarPie && cfg.textoPie
        ? `<div class="pie" style="${estiloTexto(cfg.estiloPie)}">${esc(cfg.textoPie)}</div>`
        : '';

    const paginasHTML = paginas.map(p => `
        <section class="pagina">
            <div class="grid">${p.map(etiqueta).join('')}</div>
            ${pieHTML}
        </section>`).join('');

    // Ancho máximo del pie: el área entre márgenes izquierdo y derecho
    const anchoUtil = 210 - m.izquierdo - m.derecho;

    const html = `<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="utf-8"/>
    <title>Etiquetas · AMPA</title>
    <style>
        * { box-sizing:border-box; margin:0; padding:0; }
        @page { size: A4 portrait; margin: 0; }

        html, body { background:#e9ecef; font-family: Arial, Helvetica, sans-serif; }

        .info {
            max-width:210mm; margin:6mm auto 4mm; background:white;
            padding:3mm 5mm; border-radius:4px; font-size:11px; color:#666;
        }

        .pagina {
            position:relative; width:210mm; height:297mm; overflow:hidden;
            background:white; margin:0 auto 6mm;
            box-shadow:0 1px 4px rgba(0,0,0,.25);
        }

        .grid {
            position:absolute;
            top:${m.superior}mm;
            left:${m.izquierdo}mm;
            display:grid;
            grid-template-columns:repeat(${columnas}, ${anchoMm}mm);
            grid-auto-rows:${altoMm}mm;
            gap:${sep}mm;
        }

        .etq {
            position:relative; overflow:hidden;
            width:${anchoMm}mm; height:${altoMm}mm;
            display:flex; align-items:center;
            border:1px solid #ccc; border-radius:1mm; background:white;
        }

        .txt {
            flex:1; min-width:0; position:relative; z-index:1;
            display:flex; flex-direction:column; justify-content:center;
        }

        .pie {
            position:absolute;
            left:${m.izquierdo}mm;
            bottom:${m.inferior}mm;
            max-width:${anchoUtil}mm;
        }

        @media print {
            html, body {
                background:white;
                -webkit-print-color-adjust:exact; print-color-adjust:exact;
            }
            .info { display:none; }
            .pagina { margin:0; box-shadow:none; page-break-after:always; break-after:page; }
            .pagina:last-child { page-break-after:auto; break-after:auto; }
        }
    </style>
</head>
<body>
    <div class="info">
        <b>Etiquetas AMPA</b> &nbsp;·&nbsp;
        ${nombres.length} etiqueta${nombres.length !== 1 ? 's' : ''} &nbsp;·&nbsp;
        ${anchoMm}×${altoMm} mm &nbsp;·&nbsp;
        ${columnas}×${filas} por página &nbsp;·&nbsp;
        ${paginas.length} página${paginas.length !== 1 ? 's' : ''} A4 &nbsp;·&nbsp;
        Márgenes ${m.superior}/${m.derecho}/${m.inferior}/${m.izquierdo} mm (sup/der/inf/izq).
        En el diálogo de impresión usa <b>Márgenes: Ninguno</b> o <b>Predeterminados</b> y escala 100 %.
    </div>
    ${paginasHTML}
    <script>
        window.addEventListener('load', function () {
            var imgs = document.querySelectorAll('img');
            var total = imgs.length, cargadas = 0;
            function lanzar() { setTimeout(function () { window.print(); }, 300); }
            if (total === 0) { lanzar(); return; }
            function una() { if (++cargadas >= total) lanzar(); }
            imgs.forEach(function (img) {
                if (img.complete) una();
                else { img.addEventListener('load', una); img.addEventListener('error', una); }
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
