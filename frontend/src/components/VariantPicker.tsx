import type { MenuItemVariant } from '../api/types'

interface VariantPickerProps {
  menuItemId: string
  variants: MenuItemVariant[]
  selectedId: string
  onChange: (variantId: string) => void
}

/** Radio picker for a MenuItem's variants — only rendered when variants.length > 0 (ADR-0035). */
export function VariantPicker({ menuItemId, variants, selectedId, onChange }: VariantPickerProps) {
  return (
    <fieldset className="picker">
      <legend>Wariant</legend>
      {variants.map((variant) => (
        <label key={variant.id}>
          <input
            type="radio"
            name={`variant-${menuItemId}`}
            checked={selectedId === variant.id}
            onChange={() => onChange(variant.id)}
          />
          {variant.name} ({variant.price.amount.toFixed(2)} {variant.price.currency})
        </label>
      ))}
    </fieldset>
  )
}
