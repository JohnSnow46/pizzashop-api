import { describe, it, expect, afterEach } from 'vitest'
import { render, screen, cleanup } from '@testing-library/react'
import { StaffAccountList } from './StaffAccountList'

describe('StaffAccountList', () => {
  afterEach(() => {
    cleanup()
  })

  it('shows the Polish role label instead of the raw enum value', () => {
    render(
      <StaffAccountList
        staffAccounts={[
          { id: 's1', email: 'admin@pizza.pl', role: 'RestaurantAdmin', isActive: true, createdAt: '2026-10-01T10:00:00Z' },
          { id: 's2', email: 'staff@pizza.pl', role: 'Employee', isActive: true, createdAt: '2026-10-01T10:00:00Z' },
        ]}
      />,
    )

    expect(screen.getByText('Administrator restauracji')).not.toBeNull()
    expect(screen.getByText('Pracownik')).not.toBeNull()
    expect(screen.queryByText('RestaurantAdmin')).toBeNull()
  })
})
