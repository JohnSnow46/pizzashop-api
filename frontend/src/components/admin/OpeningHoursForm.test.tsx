import { describe, it, expect, afterEach, vi } from 'vitest'
import { render, screen, cleanup } from '@testing-library/react'
import { OpeningHoursForm } from './OpeningHoursForm'

describe('OpeningHoursForm', () => {
  afterEach(cleanup)

  it('names each time input after its day', () => {
    render(
      <OpeningHoursForm
        openingHours={{
          schedule: {
            Monday: [{ start: '11:00:00', end: '22:00:00' }],
            Tuesday: [],
            Wednesday: [],
            Thursday: [],
            Friday: [],
            Saturday: [],
            Sunday: [],
          },
        }}
        onSaved={vi.fn()}
      />,
    )

    expect((screen.getByLabelText('Poniedziałek — otwarcie') as HTMLInputElement).value).toBe('11:00')
    expect((screen.getByLabelText('Poniedziałek — zamknięcie') as HTMLInputElement).value).toBe('22:00')
    expect(screen.getByLabelText('Niedziela — otwarcie')).toBeTruthy()
  })
})
