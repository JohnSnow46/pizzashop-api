import type { DayOfWeekName, RestaurantConfig, TimeRange } from '../api/types'

/**
 * JS Date#getDay() is 0 (Sunday) .. 6 (Saturday) — this maps that index onto the DayOfWeekName
 * keys used by OpeningHoursDto.schedule (which serializes DayOfWeek member names as strings).
 */
const DAY_NAMES: DayOfWeekName[] = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']

/**
 * Opening-hours windows for the given date's day of week. UI-only helper (ADR-0036, Decision
 * point 6) — the authoritative check (including the restaurant's TimeZoneId) happens in Domain.
 */
export function getWindowsForDate(config: RestaurantConfig, date: Date): TimeRange[] {
  const dayName = DAY_NAMES[date.getDay()]
  return config.openingHours.schedule[dayName] ?? []
}

function toMinutes(time: string): number {
  const [hours, minutes] = time.split(':').map(Number)
  return hours * 60 + minutes
}

/**
 * Whether `when`'s local time-of-day falls within one of that day's opening windows.
 * Orientation only — does not account for the restaurant's TimeZoneId; Domain re-validates.
 */
export function isWithinOpeningHours(config: RestaurantConfig, when: Date): boolean {
  const windows = getWindowsForDate(config, when)
  const minutesOfDay = when.getHours() * 60 + when.getMinutes()

  return windows.some((window) => minutesOfDay >= toMinutes(window.start) && minutesOfDay <= toMinutes(window.end))
}

export function isInPast(when: Date): boolean {
  return when.getTime() < Date.now()
}
