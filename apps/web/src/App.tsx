import { startTransition, useDeferredValue, useState } from 'react'
import type { FormEvent } from 'react'
import './App.css'
import { convertHl7ToCcda, type ConversionResponse } from './lib/api'

function App() {
  const [message, setMessage] = useState('')
  const [documentTitle, setDocumentTitle] = useState('Continuity of Care Document')
  const [rootTemplateOverride, setRootTemplateOverride] = useState('')
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [result, setResult] = useState<ConversionResponse | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const deferredXml = useDeferredValue(result?.ccdaXml ?? '')

  const canSubmit = Boolean(selectedFile || message.trim()) && !isSubmitting

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)

    const formData = new FormData()

    if (selectedFile) {
      formData.append('file', selectedFile)
    }

    if (message.trim()) {
      formData.append('message', message.trim())
    }

    if (documentTitle.trim()) {
      formData.append('documentTitle', documentTitle.trim())
    }

    if (rootTemplateOverride.trim()) {
      formData.append('rootTemplateOverride', rootTemplateOverride.trim())
    }

    try {
      const payload = await convertHl7ToCcda(formData)
      startTransition(() => setResult(payload))
    } catch (submissionError) {
      setResult(null)
      setError(submissionError instanceof Error ? submissionError.message : 'Conversion failed.')
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleDownload = () => {
    if (!result) {
      return
    }

    const blob = new Blob([result.ccdaXml], { type: 'application/xml' })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = result.fileName
    link.click()
    URL.revokeObjectURL(url)
  }

  return (
    <main className="shell">
      <section className="hero">
        <p className="eyebrow">HL7 to CCD</p>
        <h1>Turn HL7v2 messages into pragmatic CCD XML.</h1>
        <p className="lede">
          Upload a message file or paste raw HL7, run the reusable .NET converter, inspect warnings, and download the generated CCD.
        </p>
      </section>

      <section className="panel">
        <form className="composer" onSubmit={handleSubmit}>
          <label className="field file-field">
            <span>Upload HL7 file</span>
            <input
              aria-label="Upload HL7 file"
              type="file"
              accept=".hl7,.txt"
              onChange={(event) => setSelectedFile(event.target.files?.[0] ?? null)}
            />
            <small>{selectedFile ? selectedFile.name : 'No file selected.'}</small>
          </label>

          <label className="field">
            <span>Paste HL7 message</span>
            <textarea
              aria-label="Paste HL7 message"
              rows={12}
              value={message}
              onChange={(event) => setMessage(event.target.value)}
              placeholder="MSH|^~\&|..."
            />
          </label>

          <div className="field-grid">
            <label className="field">
              <span>Document title</span>
              <input
                aria-label="Document title"
                type="text"
                value={documentTitle}
                onChange={(event) => setDocumentTitle(event.target.value)}
              />
            </label>

            <label className="field">
              <span>Root template override</span>
              <input
                aria-label="Root template override"
                type="text"
                value={rootTemplateOverride}
                onChange={(event) => setRootTemplateOverride(event.target.value)}
                placeholder="Optional, e.g. ORU_R01"
              />
            </label>
          </div>

          <div className="actions">
            <button className="primary" type="submit" disabled={!canSubmit}>
              {isSubmitting ? 'Converting…' : 'Convert to CCD'}
            </button>
            <p className="hint">File upload takes precedence when both inputs are present.</p>
          </div>
        </form>
      </section>

      {error ? (
        <section className="feedback error" role="alert">
          <h2>Conversion failed</h2>
          <p>{error}</p>
        </section>
      ) : null}

      {result ? (
        <section className="result-grid">
          <article className="result-card">
            <div className="result-head">
              <div>
                <p className="eyebrow">Detected template</p>
                <h2>{result.detectedRootTemplate}</h2>
              </div>
              <button className="secondary" type="button" onClick={handleDownload}>
                Download XML
              </button>
            </div>

            <div className="warnings">
              <h3>Warnings</h3>
              {result.warnings.length === 0 ? (
                <p>No warnings.</p>
              ) : (
                <ul>
                  {result.warnings.map((warning) => (
                    <li key={`${warning.code}-${warning.message}`}>
                      <strong>{warning.code}</strong> {warning.message}
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </article>

          <article className="result-card xml-card">
            <div className="result-head">
              <div>
                <p className="eyebrow">CCD preview</p>
                <h2>{result.fileName}</h2>
              </div>
            </div>
            <textarea aria-label="CCD XML preview" className="xml-preview" readOnly value={deferredXml} />
          </article>
        </section>
      ) : null}
    </main>
  )
}

export default App
