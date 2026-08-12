function base64ToBlob(content) {
  const binary = atob(content)
  const bytes = new Uint8Array(binary.length)
  for (let index = 0; index < binary.length; index++) {
    bytes[index] = binary.charCodeAt(index)
  }
  return new Blob([bytes], { type: 'application/pdf' })
}

export function renderPdfDocument(previewWindow, pdfDocument) {
  previewWindow.document.title = 'Требование-накладная'
  previewWindow.document.body.replaceChildren()
  Object.assign(previewWindow.document.body.style, {
    margin: '0',
    background: '#333',
  })

  const url = URL.createObjectURL(base64ToBlob(pdfDocument.content))
  const frame = previewWindow.document.createElement('iframe')
  frame.src = url
  frame.title = 'Требование-накладная'
  Object.assign(frame.style, {
    display: 'block',
    width: '100%',
    height: '100vh',
    border: '0',
  })
  previewWindow.document.body.append(frame)
  previewWindow.addEventListener('beforeunload', () => {
    URL.revokeObjectURL(url)
  })
}
