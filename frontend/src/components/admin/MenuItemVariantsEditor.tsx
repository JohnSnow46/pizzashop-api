import type { MenuItemVariantInput } from '../../api/types'

interface MenuItemVariantsEditorProps {
  variants: MenuItemVariantInput[]
  currency: string
  onChange: (variants: MenuItemVariantInput[]) => void
}

/**
 * Variant editor for MenuItemForm. Enforces ADR-0016 up front in local form state, instead of
 * just handling the backend's 400 on an invalid submit:
 * - a radio-button-per-row keeps exactly one variant marked default at all times (never a
 *   zero-default state);
 * - "remove" is disabled when this is the only remaining variant (CannotRemoveLastVariantException),
 *   or when this row is the default and more than one variant remains
 *   (InvalidVariantConfigurationException — the admin must pick a different default first).
 */
export function MenuItemVariantsEditor({ variants, currency, onChange }: MenuItemVariantsEditorProps) {
  function updateRow(index: number, patch: Partial<MenuItemVariantInput>) {
    onChange(variants.map((variant, i) => (i === index ? { ...variant, ...patch } : variant)))
  }

  function setDefault(index: number) {
    onChange(variants.map((variant, i) => ({ ...variant, isDefault: i === index })))
  }

  function addVariant() {
    onChange([...variants, { id: null, name: '', price: { amount: 0, currency }, isDefault: variants.length === 0 }])
  }

  function removeVariant(index: number) {
    onChange(variants.filter((_, i) => i !== index))
  }

  function removeDisabledReason(variant: MenuItemVariantInput): string | null {
    if (variants.length === 1) {
      return 'Nie można usunąć jedynego wariantu.'
    }
    if (variant.isDefault) {
      return 'Najpierw ustaw inny wariant jako domyślny.'
    }
    return null
  }

  return (
    <div className="variant-editor">
      {variants.map((variant, index) => {
        const disabledReason = removeDisabledReason(variant)
        return (
          <div className="variant-row" key={variant.id ?? `new-${index}`}>
            <label className="variant-row__default">
              <input type="radio" name="variant-default" checked={variant.isDefault} onChange={() => setDefault(index)} />
              Domyślny
            </label>
            <input
              className="variant-row__name"
              placeholder="Nazwa wariantu"
              value={variant.name}
              onChange={(e) => updateRow(index, { name: e.target.value })}
            />
            <input
              className="variant-row__price"
              type="number"
              step="0.01"
              min="0"
              value={variant.price.amount}
              onChange={(e) => updateRow(index, { price: { ...variant.price, amount: Number(e.target.value) } })}
            />
            <span className="variant-row__currency">{variant.price.currency}</span>
            <button
              type="button"
              className="queue-action-btn queue-action-btn--danger"
              disabled={disabledReason !== null}
              title={disabledReason ?? undefined}
              onClick={() => removeVariant(index)}
            >
              Usuń
            </button>
          </div>
        )
      })}

      <button type="button" onClick={addVariant}>
        Dodaj wariant
      </button>
    </div>
  )
}
