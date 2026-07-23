import type { Ingredient } from '../api/types'

interface ExtrasPickerProps {
  extras: Ingredient[]
  selectedIds: string[]
  onChange: (extraIds: string[]) => void
}

/** Checkbox picker for a MenuItem's AllowedExtras. Unavailable ingredients are shown disabled. */
export function ExtrasPicker({ extras, selectedIds, onChange }: ExtrasPickerProps) {
  function toggle(id: string) {
    if (selectedIds.includes(id)) {
      onChange(selectedIds.filter((existing) => existing !== id))
    } else {
      onChange([...selectedIds, id])
    }
  }

  return (
    <fieldset className="picker">
      <legend>Dodatki</legend>
      {extras.map((extra) => (
        <label key={extra.id}>
          <input
            type="checkbox"
            disabled={!extra.isAvailable}
            checked={selectedIds.includes(extra.id)}
            onChange={() => toggle(extra.id)}
          />
          {extra.name} (+{extra.extraPrice.amount.toFixed(2)} {extra.extraPrice.currency})
          {!extra.isAvailable && ' — niedostępny'}
        </label>
      ))}
    </fieldset>
  )
}
