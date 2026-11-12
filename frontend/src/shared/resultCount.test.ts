import { describe, it, expect } from 'vitest'
import { formatResultCount } from './resultCount'

describe('formatResultCount', () => {
  it.each([
    [0, 'Brak wyników'],
    [1, 'Znaleziono 1 pozycję'],
    [2, 'Znaleziono 2 pozycje'],
    [4, 'Znaleziono 4 pozycje'],
    [5, 'Znaleziono 5 pozycji'],
    [12, 'Znaleziono 12 pozycji'],
    [14, 'Znaleziono 14 pozycji'],
    [22, 'Znaleziono 22 pozycje'],
    [111, 'Znaleziono 111 pozycji'],
  ])('%i -> %s', (count, expected) => {
    expect(formatResultCount(count)).toBe(expected)
  })
})
