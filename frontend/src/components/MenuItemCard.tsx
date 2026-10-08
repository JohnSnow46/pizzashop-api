import { useEffect, useRef, useState } from 'react'
import type { MenuItem } from '../api/types'
import { buildCartItemKey } from '../cart/types'
import { useCart } from '../hooks/useCart'
import { useMenuItemReviews } from '../hooks/useMenuItemReviews'
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
  // Purely presentational: briefly flags the button as "just added" for a subtle
  // confirmation animation (Faza 2 accent) — no effect on cart state/business logic.
  const [justAdded, setJustAdded] = useState(false)
  const justAddedTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const [reviewsOpen, setReviewsOpen] = useState(false)
  const reviews = useMenuItemReviews(item.id, reviewsOpen)

  useEffect(() => {
    return () => {
      if (justAddedTimeoutRef.current) {
        clearTimeout(justAddedTimeoutRef.current)
      }
    }
  }, [])

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

    setJustAdded(true)
    if (justAddedTimeoutRef.current) {
      clearTimeout(justAddedTimeoutRef.current)
    }
    justAddedTimeoutRef.current = setTimeout(() => setJustAdded(false), 700)
  }

  return (
    <article className={`menu-card${item.isAvailable ? '' : ' unavailable'}`}>
      {item.imageUrl && <img src={item.imageUrl} alt={item.name} />}
      <h3>{item.name}</h3>
      {!item.isAvailable && <span className="menu-card__unavailable-badge">Niedostępne</span>}
      {item.description && <p className="menu-card__description">{item.description}</p>}

      <div className="menu-card__rating">
        {item.averageRating !== null ? (
          <button type="button" className="menu-card__rating-toggle" onClick={() => setReviewsOpen((open) => !open)}>
            ★ {item.averageRating.toFixed(1)} ({item.reviewCount})
          </button>
        ) : (
          <span className="menu-card__rating-empty">Brak ocen</span>
        )}
      </div>

      {reviewsOpen && (
        <div className="menu-card__reviews">
          {reviews.loading && <p className="checkout-hint">Ładowanie recenzji…</p>}
          {reviews.error && <p className="checkout-error">{reviews.error}</p>}
          {reviews.data && reviews.data.reviews.length === 0 && <p className="checkout-hint">Brak recenzji.</p>}
          {reviews.data && reviews.data.reviews.length > 0 && (
            <ul className="menu-card__reviews-list">
              {reviews.data.reviews.map((review) => (
                <li key={review.id}>
                  <strong>★ {review.rating}</strong>
                  {review.comment && <span> — {review.comment}</span>}
                </li>
              ))}
            </ul>
          )}
        </div>
      )}

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

      <button
        type="button"
        className={`add-to-cart-btn${justAdded ? ' add-to-cart-btn--added' : ''}`}
        disabled={!item.isAvailable}
        onClick={handleAddToCart}
      >
        {justAdded ? 'Dodano ✓' : 'Dodaj do koszyka'}
      </button>
    </article>
  )
}
