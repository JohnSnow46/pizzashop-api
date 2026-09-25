import { describe, it, expect } from 'vitest'
import {
  validatePhoneNumber,
  validateEmailOptional,
  validateFullName,
  validateContact,
  validateAddress,
  validatePromotionCode,
} from './validation'
import type { Address, ContactDetails } from '../api/types'

describe('validatePhoneNumber', () => {
  it('accepts a plain 9-digit number', () => {
    expect(validatePhoneNumber('123456789')).toBeNull()
  })

  it('accepts a number with country code and separators', () => {
    expect(validatePhoneNumber('+48 123-456-789')).toBeNull()
  })

  it('accepts a number with country code and no separators', () => {
    expect(validatePhoneNumber('+48123456789')).toBeNull()
  })

  it('rejects an empty string', () => {
    expect(validatePhoneNumber('')).not.toBeNull()
  })

  it('rejects a number with too few digits', () => {
    expect(validatePhoneNumber('12345')).not.toBeNull()
  })

  it('rejects letters', () => {
    expect(validatePhoneNumber('abcdefghi')).not.toBeNull()
  })
})

describe('validateEmailOptional', () => {
  it('returns null for null', () => {
    expect(validateEmailOptional(null)).toBeNull()
  })

  it('returns null for empty string', () => {
    expect(validateEmailOptional('')).toBeNull()
  })

  it('returns null for a valid email', () => {
    expect(validateEmailOptional('user@example.com')).toBeNull()
  })

  it('returns an error for an invalid email', () => {
    expect(validateEmailOptional('not-an-email')).not.toBeNull()
  })
})

describe('validateFullName', () => {
  it('returns an error for an empty string', () => {
    expect(validateFullName('')).not.toBeNull()
  })

  it('returns an error for whitespace only', () => {
    expect(validateFullName('   ')).not.toBeNull()
  })

  it('returns null for a non-empty name', () => {
    expect(validateFullName('Jan Kowalski')).toBeNull()
  })

  it('returns an error for a name over 200 characters', () => {
    expect(validateFullName('a'.repeat(201))).not.toBeNull()
  })

  it('returns null for a name at exactly 200 characters', () => {
    expect(validateFullName('a'.repeat(200))).toBeNull()
  })
})

describe('validatePromotionCode', () => {
  it('returns null for an empty string', () => {
    expect(validatePromotionCode('')).toBeNull()
  })

  it('returns null for a code at exactly 50 characters', () => {
    expect(validatePromotionCode('a'.repeat(50))).toBeNull()
  })

  it('returns an error for a code over 50 characters', () => {
    expect(validatePromotionCode('a'.repeat(51))).not.toBeNull()
  })
})

describe('validateContact', () => {
  it('returns only the email key when phone is valid and email is invalid', () => {
    const contact: ContactDetails = {
      fullName: 'Jan Kowalski',
      phoneNumber: '123456789',
      email: 'not-an-email',
    }
    const errors = validateContact(contact)
    expect(Object.keys(errors)).toEqual(['email'])
  })

  it('returns no errors for a fully valid contact', () => {
    const contact: ContactDetails = {
      fullName: 'Jan Kowalski',
      phoneNumber: '123456789',
      email: null,
    }
    expect(validateContact(contact)).toEqual({})
  })
})

describe('validateAddress', () => {
  const validAddress: Address = {
    street: 'Main St',
    buildingNumber: '1',
    city: 'Warsaw',
    postalCode: '00-001',
    apartmentNumber: null,
    notes: null,
  }

  it('returns an error for a missing street', () => {
    const errors = validateAddress({ ...validAddress, street: '' })
    expect(errors.street).toBeDefined()
  })

  it('returns an error for a missing building number', () => {
    const errors = validateAddress({ ...validAddress, buildingNumber: '' })
    expect(errors.buildingNumber).toBeDefined()
  })

  it('returns an error for a missing city', () => {
    const errors = validateAddress({ ...validAddress, city: '' })
    expect(errors.city).toBeDefined()
  })

  it('accepts a valid postal code', () => {
    const errors = validateAddress({ ...validAddress, postalCode: '00-001' })
    expect(errors.postalCode).toBeUndefined()
  })

  it('rejects a postal code with the wrong format with a distinct message from empty', () => {
    const wrongFormatErrors = validateAddress({ ...validAddress, postalCode: '00001' })
    const emptyErrors = validateAddress({ ...validAddress, postalCode: '' })
    expect(wrongFormatErrors.postalCode).toBeDefined()
    expect(emptyErrors.postalCode).toBeDefined()
    expect(wrongFormatErrors.postalCode).not.toEqual(emptyErrors.postalCode)
  })

  it('returns an error for a street over 200 characters', () => {
    const errors = validateAddress({ ...validAddress, street: 'a'.repeat(201) })
    expect(errors.street).toBeDefined()
  })

  it('returns an error for a building number over 20 characters', () => {
    const errors = validateAddress({ ...validAddress, buildingNumber: 'a'.repeat(21) })
    expect(errors.buildingNumber).toBeDefined()
  })

  it('returns an error for a city over 100 characters', () => {
    const errors = validateAddress({ ...validAddress, city: 'a'.repeat(101) })
    expect(errors.city).toBeDefined()
  })

  it('returns an error for notes over 500 characters', () => {
    const errors = validateAddress({ ...validAddress, notes: 'a'.repeat(501) })
    expect(errors.notes).toBeDefined()
  })

  it('accepts notes at exactly 500 characters', () => {
    const errors = validateAddress({ ...validAddress, notes: 'a'.repeat(500) })
    expect(errors.notes).toBeUndefined()
  })
})
