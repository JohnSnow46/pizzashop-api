import { useState } from 'react'
import type { MenuItem } from '../api/types'
import { buildCartItemKey } from '../cart/types'
import { useCart } from '../hooks/useCart'
import { ExtrasPicker } from './ExtrasPicker'
import { VariantPicker } from './VariantPicker'

interface MenuItemCardProps {
  item: MenuItem
}

function defaultVariantId(item: MenuItem): string {
  if (item.variants.length === 0) {
    return ''
  }
  return item.variants.find((v) => v.isDefault)?.id ?? item.variants[0].id
}

export function MenuItemCard({ item }: MenuItemCardProps) {
  const { addItem } = useCart()
  const [selectedVariantId, setSelectedVariantId] = useState(() => defaultVariantId(item))
  const [selectedExtraIds, setSelectedExtraIds] = useState<string[]>([])

  const selectedVariant = item.variants.find((v) => v.id === selectedVariantId) ?? null
  const selectedExtras = item.allowedExtras.filter((extra) => selectedExtraIds.includes(extra.id))

  const unitPriceAmount =
    (selectedVariant?.price.amount ?? item.basePrice.amount) +
    selectedExtras.reduce((sum, extra) => sum + extra.extraPrice.amount, 0)

  function handleAddToCart() {
    addItem({
      key: buildCartItemKey(item.id, selectedVariant?.id ?? null, selectedExtraIds),
      menuItemId: item.id,
      menuItemName: item.name,
      variantId: selectedVariant?.id ?? null,
      variantName: selectedVariant?.name ?? null,
      extraIds: selectedExtraIds,
      extraNames: selectedExtras.map((extra) => extra.name),
      unitPriceAmount,
      currency: item.basePrice.currency,
      quantity: 1,
    })
  }

  return (
    <article className={`menu-card${item.isAvailable ? '' : ' unavailable'}`}>
      {item.imageUrl && <img src={item.imageUrl} alt={item.name} />}
      <h3>{item.name}</h3>
      {!item.isAvailable && <span className="menu-card__unavailable-badge">Niedostępne</span>}
      {item.description && <p className="menu-card__description">{item.description}</p>}

      {item.variants.length > 0 && (
        <VariantPicker
          menuItemId={item.id}
          variants={item.variants}
          selectedId={selectedVariantId}
          onChange={setSelectedVariantId}
        />
      )}

      {item.allowedExtras.length > 0 && (
        <ExtrasPicker extras={item.allowedExtras} selectedIds={selectedExtraIds} onChange={setSelectedExtraIds} />
      )}

      <p className="menu-card__price">
        {unitPriceAmount.toFixed(2)} {item.basePrice.currency}
      </p>

      <button type="button" className="add-to-cart-btn" disabled={!item.isAvailable} onClick={handleAddToCart}>
        Dodaj do koszyka
      </button>
    </article>
  )
}
