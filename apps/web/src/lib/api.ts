export interface ConversionWarning {
  code: string
  message: string
}

export interface ConversionResponse {
  fileName: string
  detectedRootTemplate: string
  warnings: ConversionWarning[]
  ccdaXml: string
}

interface ApiErrorResponse {
  code: string
  message: string
}

const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(/\/$/, '') ?? 'http://localhost:5080'

export async function convertHl7ToCcda(formData: FormData): Promise<ConversionResponse> {
  const response = await fetch(`${apiBaseUrl}/api/convert/ccd`, {
    method: 'POST',
    body: formData,
  })

  if (!response.ok) {
    const errorPayload = (await response.json()) as ApiErrorResponse
    throw new Error(errorPayload.message)
  }

  return (await response.json()) as ConversionResponse
}
