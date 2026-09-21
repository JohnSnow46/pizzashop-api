import type { Address, ContactDetails } from '../api/types'
import { EMAIL_PATTERN } from '../shared/validation'

/**
 * Same pattern as PizzaShop.Application.Orders.Validators.CreateOrderCommandValidator
 * (PhoneNumberPattern): optional '+48' country code, optional space/dash separators between
 * three-digit groups (e.g. "123456789", "+48 123-456-789").
 */
const PHONE_NUMBER_PATTERN = /^(\+48[\s-]?)?\d{3}([\s-]?\d{3}){2}$/

/** Loose PL postal code check (NN-NNN) — soft, UI-only; the backend does not enforce this format. */
const POSTAL_CODE_PATTERN = /^\d{2}-\d{3}$/

/** Mirrors CreateOrderCommandValidator: required, max 200 chars. */
export function validateFullName(fullName: string): string | null {
  if (fullName.trim().length === 0) {
    return 'Podaj imię i nazwisko.'
  }
  return fullName.trim().length > 200 ? 'Imię i nazwisko może mieć maksymalnie 200 znaków.' : null
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

/**
 * Length limits mirror CreateOrderCommandValidator's DeliveryAddress rules: same
 * DB-mapped Address value object, so the same maximums apply here.
 */
export function validateAddress(address: Address): Record<string, string> {
  const errors: Record<string, string> = {}

  if (address.street.trim().length === 0) {
    errors.street = 'Podaj ulicę.'
  } else if (address.street.trim().length > 200) {
    errors.street = 'Ulica może mieć maksymalnie 200 znaków.'
  }

  if (address.buildingNumber.trim().length === 0) {
    errors.buildingNumber = 'Podaj numer budynku.'
  } else if (address.buildingNumber.trim().length > 20) {
    errors.buildingNumber = 'Numer budynku może mieć maksymalnie 20 znaków.'
  }

  if (address.city.trim().length === 0) {
    errors.city = 'Podaj miasto.'
  } else if (address.city.trim().length > 100) {
    errors.city = 'Miasto może mieć maksymalnie 100 znaków.'
  }

  if (address.postalCode.trim().length === 0) {
    errors.postalCode = 'Podaj kod pocztowy.'
  } else if (!POSTAL_CODE_PATTERN.test(address.postalCode.trim())) {
    errors.postalCode = 'Kod pocztowy powinien mieć format NN-NNN (np. 00-001).'
  }

  if ((address.apartmentNumber ?? '').trim().length > 20) {
    errors.apartmentNumber = 'Numer lokalu może mieć maksymalnie 20 znaków.'
  }

  if ((address.notes ?? '').trim().length > 500) {
    errors.notes = 'Notatka może mieć maksymalnie 500 znaków.'
  }

  return errors
}
