// Thin fetch wrapper. Base URL is "/api" — in dev, Vite's proxy (vite.config.ts) forwards
// that to the PizzaShop.Api dev server; in a production build, the app must be served from
// the same origin as the Api (or a reverse proxy set up the same way), otherwise switch this
// to an absolute URL plus the "frontend" CORS policy (ADR-0035).
const BASE_URL = '/api'

/**
 * Shape of ASP.NET Core's ProblemDetails/ValidationProblemDetails bodies (ExceptionHandler,
 * ADR-0027), as far as this client cares about.
 */
interface ProblemDetailsBody {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  readonly status: number
  /** ProblemDetails "title", when the error body was parseable JSON (ADR-0036, point 7). */
  readonly title?: string
  /** ProblemDetails "detail", when the error body was parseable JSON. */
  readonly detail?: string
  /** ValidationProblemDetails "errors" map (field -> messages), present for 400 responses. */
  readonly errors?: Record<string, string[]>

  constructor(status: number, message: string, problem?: ProblemDetailsBody) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.title = problem?.title
    this.detail = problem?.detail
    this.errors = problem?.errors
  }
}

async function toApiError(path: string, response: Response): Promise<ApiError> {
  const text = await response.text().catch(() => '')

  let problem: ProblemDetailsBody | undefined
  if (text) {
    try {
      problem = JSON.parse(text) as ProblemDetailsBody
    } catch {
      problem = undefined
    }
  }

  const message = problem?.detail ?? problem?.title ?? text ?? `Request to ${path} failed with status ${response.status}`
  return new ApiError(response.status, message, problem)
}

async function get<T>(path: string): Promise<T> {
  const response = await fetch(`${BASE_URL}${path}`)

  if (!response.ok) {
    throw await toApiError(path, response)
  }

  return (await response.json()) as T
}

async function post<T>(path: string, body: unknown): Promise<T> {
  const response = await fetch(`${BASE_URL}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })

  if (!response.ok) {
    throw await toApiError(path, response)
  }

  return (await response.json()) as T
}

export const apiClient = { get, post }
