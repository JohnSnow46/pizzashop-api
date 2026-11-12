/**
 * "Znaleziono 3 pozycje" -- the filter result count read out by screen readers. Polish
 * picks the noun form by the number: 1 pozycja, 2-4 pozycje (but not 12-14), else pozycji.
 */
export function formatResultCount(count: number): string {
  if (count === 0) return 'Brak wyników'
  if (count === 1) return 'Znaleziono 1 pozycję'
  const lastDigit = count % 10
  const lastTwo = count % 100
  const few = lastDigit >= 2 && lastDigit <= 4 && (lastTwo < 12 || lastTwo > 14)
  return `Znaleziono ${count} ${few ? 'pozycje' : 'pozycji'}`
}
