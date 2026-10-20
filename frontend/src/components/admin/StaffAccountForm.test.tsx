import { describe, it, expect, afterEach, beforeEach, vi } from 'vitest'
import { render, screen, cleanup, fireEvent, waitFor } from '@testing-library/react'
import { StaffAccountForm } from './StaffAccountForm'

vi.mock('../../api/staffApi', () => ({
  createStaffAccount: vi.fn(),
}))

import { createStaffAccount } from '../../api/staffApi'

const createStaffAccountMock = vi.mocked(createStaffAccount)

function fill(email: string, password: string) {
  fireEvent.change(screen.getByLabelText('Email'), { target: { value: email } })
  fireEvent.change(screen.getByLabelText('Hasło'), { target: { value: password } })
  fireEvent.click(screen.getByText('Zapisz'))
}

describe('StaffAccountForm', () => {
  beforeEach(() => {
    createStaffAccountMock.mockReset()
  })

  afterEach(() => {
    cleanup()
  })

  it('rejects a password without a digit before calling the API', () => {
    render(<StaffAccountForm onCreated={vi.fn()} onCancel={vi.fn()} />)

    fill('staff@pizza.pl', 'onlyletters')

    expect(screen.getByText('Hasło musi zawierać co najmniej jedną literę i jedną cyfrę.')).not.toBeNull()
    expect(createStaffAccountMock).not.toHaveBeenCalled()
  })

  it('rejects a too-short password before calling the API', () => {
    render(<StaffAccountForm onCreated={vi.fn()} onCancel={vi.fn()} />)

    fill('staff@pizza.pl', 'abc1')

    expect(screen.getByText('Hasło musi mieć od 8 do 100 znaków.')).not.toBeNull()
    expect(createStaffAccountMock).not.toHaveBeenCalled()
  })

  it('submits a valid account', async () => {
    createStaffAccountMock.mockResolvedValue(undefined as never)
    const onCreated = vi.fn()
    render(<StaffAccountForm onCreated={onCreated} onCancel={vi.fn()} />)

    fill('staff@pizza.pl', 'secret123')

    await waitFor(() =>
      expect(createStaffAccountMock).toHaveBeenCalledWith({ email: 'staff@pizza.pl', password: 'secret123', role: 'Employee' }),
    )
    expect(onCreated).toHaveBeenCalled()
  })

  it('caps the email at the backend MaximumLength (200 chars)', () => {
    render(<StaffAccountForm onCreated={vi.fn()} onCancel={vi.fn()} />)

    expect(screen.getByLabelText('Email').getAttribute('maxlength')).toBe('200')
  })
})
