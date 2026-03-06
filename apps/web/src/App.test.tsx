import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import App from './App'

describe('App', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('submits an uploaded file and renders the conversion result', async () => {
    const user = userEvent.setup()
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(
        JSON.stringify({
          fileName: 'ORU_R01.ccda.xml',
          detectedRootTemplate: 'ORU_R01',
          warnings: [{ code: 'MissingAuthor', message: 'No Practitioner resource was available.' }],
          ccdaXml: '<ClinicalDocument><title>Test CCD</title></ClinicalDocument>',
        }),
        { status: 200, headers: { 'Content-Type': 'application/json' } },
      ),
    )

    render(<App />)

    const fileInput = screen.getByLabelText(/upload hl7 file/i)
    const file = new File(['MSH|^~\\&|SEND'], 'sample.hl7', { type: 'text/plain' })
    await user.upload(fileInput, file)
    await user.click(screen.getByRole('button', { name: /convert to ccd/i }))

    await waitFor(() => expect(fetchSpy).toHaveBeenCalledTimes(1))
    const [, options] = fetchSpy.mock.calls[0]
    expect(options?.body).toBeInstanceOf(FormData)
    expect((options?.body as FormData).get('file')).toBe(file)
    expect(await screen.findByText('ORU_R01')).toBeInTheDocument()
    expect(screen.getByDisplayValue(/ClinicalDocument/)).toBeInTheDocument()
  })

  it('shows an API error message', async () => {
    const user = userEvent.setup()
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(
        JSON.stringify({
          code: 'InvalidHl7Message',
          message: 'The HL7 message is invalid.',
        }),
        { status: 400, headers: { 'Content-Type': 'application/json' } },
      ),
    )

    render(<App />)

    await user.type(screen.getByLabelText(/paste hl7 message/i), 'not-an-hl7-message')
    await user.click(screen.getByRole('button', { name: /convert to ccd/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent('The HL7 message is invalid.')
  })

  it('downloads the generated XML', async () => {
    const user = userEvent.setup()
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(
        JSON.stringify({
          fileName: 'ADT_A01.ccda.xml',
          detectedRootTemplate: 'ADT_A01',
          warnings: [],
          ccdaXml: '<ClinicalDocument><title>Test CCD</title></ClinicalDocument>',
        }),
        { status: 200, headers: { 'Content-Type': 'application/json' } },
      ),
    )
    const createObjectUrlSpy = vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:download')
    const revokeObjectUrlSpy = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {})
    const clickSpy = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {})

    render(<App />)

    await user.type(screen.getByLabelText(/paste hl7 message/i), 'MSH|^~\\&|SEND')
    await user.click(screen.getByRole('button', { name: /convert to ccd/i }))
    await user.click(await screen.findByRole('button', { name: /download xml/i }))

    expect(createObjectUrlSpy).toHaveBeenCalledTimes(1)
    expect(clickSpy).toHaveBeenCalledTimes(1)
    expect(revokeObjectUrlSpy).toHaveBeenCalledWith('blob:download')
  })
})
