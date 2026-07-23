import type { Address, ContactDetails } from '../api/types'

/**
 * Same pattern as PizzaShop.Application.Orders.Validators.CreateOrderCommandValidator
 * (PhoneNumberPattern): optional '+48' country code, optional space/dash separators between
 * three-digit groups (e.g. "123456789", "+48 123-456-789").
 */
const PHONE_NUMBER_PATTERN = /^(\+48[\s-]?)?\d{3}([\s-]?\d{3}){2}$/

/** Loose PL postal code check (NN-NNN) — soft, UI-only; the backend does not enforce this format. */
const POSTAL_CODE_PATTERN = /^\d{2}-\d{3}$/

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export function validateFullName(fullName: string): string | null {
  return fullName.trim().length === 0 ? 'Podaj imię i nazwisko.' : null
}

export function validatePhoneNumber(phoneNumber: string): string | null {
  if (phoneNumber.trim().length === 0) {
    return 'Podaj numer telefonu.'
  }
  return PHONE_NUMBER_PATTERN.test(phoneNumber.trim())
    ? null
    : 'Numer telefonu musi być poprawnym polskim numerem (np. "123456789" lub "+48 123-456-789").'
}

export function validateEmailOptional(email: string | null): string | null {
  if (!email || email.trim().length === 0) {
    return null
  }
  return EMAIL_PATTERN.test(email.trim()) ? null : 'Podaj poprawny adres e-mail.'
}

export function validateContact(contact: ContactDetails): Record<string, string> {
  const errors: Record<string, string> = {}

  const fullNameError = validateFullName(contact.fullName)
  if (fullNameError) errors.fullName = fullNameError

  const phoneError = validatePhoneNumber(contact.phoneNumber)
  if (phoneError) errors.phoneNumber = phoneError

  const emailError = validateEmailOptional(contact.email)
  if (emailError) errors.email = emailError

  return errors
}

export function validateAddress(address: Address): Record<string, string> {
  const errors: Record<string, string> = {}

  if (address.street.trim().length === 0) errors.street = 'Podaj ulicę.'
  if (address.buildingNumber.trim().length === 0) errors.buildingNumber = 'Podaj numer budynku.'
  if (address.city.trim().length === 0) errors.city = 'Podaj miasto.'

  if (address.postalCode.trim().length === 0) {
    errors.postalCode = 'Podaj kod pocztowy.'
  } else if (!POSTAL_CODE_PATTERN.test(address.postalCode.trim())) {
    errors.postalCode = 'Kod pocztowy powinien mieć format NN-NNN (np. 00-001).'
  }

  return errors
}
