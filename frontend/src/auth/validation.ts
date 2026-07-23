// Loose client-side mirror of backend validators (ADR-0037), same pattern as
// checkout/validation.ts. Domain rules (e.g. "email already registered") stay server-side —
// this only checks shape, so the user gets fast feedback before a round-trip.
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export function validateEmail(email: string): string | null {
  if (email.trim().length === 0) {
    return 'Podaj adres e-mail.'
  }
  return EMAIL_PATTERN.test(email.trim()) ? null : 'Podaj poprawny adres e-mail.'
}

/** Login has no strength requirement server-side — just "not empty" (RegisterCustomerCommandValidator differs, see validateNewPassword). */
export function validateLoginPassword(password: string): string | null {
  return password.length === 0 ? 'Podaj hasło.' : null
}

/** Mirrors RegisterCustomerCommandValidator: 8-100 chars, at least one letter and one digit. */
export function validateNewPassword(password: string): string | null {
  if (password.length === 0) {
    return 'Podaj hasło.'
  }
  if (password.length < 8 || password.length > 100) {
    return 'Hasło musi mieć od 8 do 100 znaków.'
  }
  if (!/[A-Za-z]/.test(password) || !/[0-9]/.test(password)) {
    return 'Hasło musi zawierać co najmniej jedną literę i jedną cyfrę.'
  }
  return null
}

/** Mirrors RegisterCustomerCommandValidator: required, max 200 chars. */
export function validateFullName(fullName: string): string | null {
  if (fullName.trim().length === 0) {
    return 'Podaj imię i nazwisko.'
  }
  return fullName.trim().length > 200 ? 'Imię i nazwisko może mieć maksymalnie 200 znaków.' : null
}

/** Mirrors RegisterCustomerCommandValidator: optional, max 30 chars. */
export function validatePhoneNumberOptional(phoneNumber: string): string | null {
  return phoneNumber.trim().length > 30 ? 'Numer telefonu może mieć maksymalnie 30 znaków.' : null
}
