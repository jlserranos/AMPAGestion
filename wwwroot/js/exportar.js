// ── Exportar tabla a PDF ───────────────────────────────────────────────
window.exportarPDF = function (titulo, columnas, filas) {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF({ orientation: 'landscape', unit: 'mm', format: 'a4' });

    // Cabecera
    doc.setFontSize(14);
    doc.setFont('helvetica', 'bold');
    doc.text(titulo, 14, 16);

    doc.setFontSize(9);
    doc.setFont('helvetica', 'normal');
    doc.setTextColor(120);
    doc.text('AMPA Gestión · ' + new Date().toLocaleDateString('es-ES'), 14, 22);
    doc.setTextColor(0);

    // Tabla
    doc.autoTable({
        head: [columnas],
        body: filas,
        startY: 26,
        styles: { fontSize: 8, cellPadding: 2 },
        headStyles: { fillColor: [13, 110, 253], textColor: 255, fontStyle: 'bold' },
        alternateRowStyles: { fillColor: [245, 247, 250] },
        margin: { left: 14, right: 14 }
    });

    // Pie de página
    const totalPaginas = doc.internal.getNumberOfPages();
    for (let i = 1; i <= totalPaginas; i++) {
        doc.setPage(i);
        doc.setFontSize(8);
        doc.setTextColor(150);
        doc.text(`Página ${i} de ${totalPaginas}`, doc.internal.pageSize.width - 30, doc.internal.pageSize.height - 8);
    }

    doc.save(titulo.replace(/\s+/g, '_') + '_' + fechaHoy() + '.pdf');
};

// ── Exportar tabla a Excel ─────────────────────────────────────────────
window.exportarExcel = function (titulo, columnas, filas) {
    const wb = XLSX.utils.book_new();
    const datos = [columnas, ...filas];
    const ws = XLSX.utils.aoa_to_sheet(datos);

    // Ancho automático de columnas
    const anchos = columnas.map((col, i) => ({
        wch: Math.max(col.length, ...filas.map(f => String(f[i] ?? '').length), 10)
    }));
    ws['!cols'] = anchos;

    XLSX.utils.book_append_sheet(wb, ws, titulo.substring(0, 31));
    XLSX.writeFile(wb, titulo.replace(/\s+/g, '_') + '_' + fechaHoy() + '.xlsx');
};

// ── Imprimir tabla ─────────────────────────────────────────────────────
window.imprimirTabla = function (titulo, columnas, filas) {
    const ventana = window.open('', '_blank');
    const filaHtml = filas.map(f =>
        `<tr>${f.map(c => `<td>${c ?? ''}</td>`).join('')}</tr>`
    ).join('');

    ventana.document.write(`
        <!DOCTYPE html>
        <html lang="es">
        <head>
            <meta charset="utf-8"/>
            <title>${titulo}</title>
            <style>
                body { font-family: Arial, sans-serif; font-size: 11px; margin: 20px; }
                h2   { color: #0d6efd; margin-bottom: 4px; }
                p    { color: #666; font-size: 10px; margin: 0 0 12px; }
                table { width: 100%; border-collapse: collapse; }
                th   { background: #0d6efd; color: white; padding: 6px 8px; text-align: left; font-size: 10px; }
                td   { padding: 5px 8px; border-bottom: 1px solid #eee; }
                tr:nth-child(even) td { background: #f8f9fa; }
                @media print { @page { margin: 15mm; } }
            </style>
        </head>
        <body>
            <h2>${titulo}</h2>
            <p>AMPA Gestión · ${new Date().toLocaleDateString('es-ES')}</p>
            <table>
                <thead><tr>${columnas.map(c => `<th>${c}</th>`).join('')}</tr></thead>
                <tbody>${filaHtml}</tbody>
            </table>
        </body>
        </html>`);
    ventana.document.close();
    ventana.focus();
    setTimeout(() => { ventana.print(); ventana.close(); }, 400);
};

function fechaHoy() {
    return new Date().toISOString().slice(0, 10);
}
