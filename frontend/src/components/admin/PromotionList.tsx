import { useMemo, useState } from 'react'
import { deactivatePromotion } from '../../api/promotionsApi'
import type { Promotion } from '../../api/types'
import { promotionStatus } from './promotionStatus'

interface PromotionListProps {
  promotions: Promotion[]
  onEdit: (promotion: Promotion) => void
  /** Called after a successful deactivation so the caller can refetch the list. */
  onDeactivated: () => void
}

const TYPE_LABELS: Record<Promotion['type'], string> = {
  Percentage: 'Procentowa',
  FixedAmount: 'Kwota stała',
  FreeDelivery: 'Darmowa dostawa',
  BuyXGetY: 'Kup X dostań Y',
}

function formatBaseValue(promotion: Promotion): string {
  if (promotion.type === 'Percentage') {
    return promotion.value !== null ? `${promotion.value}%` : '—'
  }
  if (promotion.type === 'FixedAmount') {
    return promotion.value !== null ? `${promotion.value.toFixed(2)} PLN` : '—'
  }
  if (promotion.type === 'BuyXGetY' && promotion.buyXGetY) {
    const rule = promotion.buyXGetY
    return `Kup ${rule.buyQuantity}, ${rule.getQuantity} taniej o ${rule.rewardDiscountPercentage}%`
  }
  return '—'
}

/** Base value plus the minimum order value, when set -- previously BuyXGetY/FreeDelivery rows
 * and every minOrderValue were simply invisible in this table, only in the edit form. */
function formatValue(promotion: Promotion): string {
  const base = formatBaseValue(promotion)
  if (!promotion.minOrderValue) {
    return base
  }
  const minimum = `od ${promotion.minOrderValue.amount.toFixed(2)} ${promotion.minOrderValue.currency}`
  return base === '—' ? minimum : `${base}, ${minimum}`
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('pl-PL')
}

/** Read-only table of all promotions with an edit action and a (one-way) deactivate action. */
export function PromotionList({ promotions, onEdit, onDeactivated }: PromotionListProps) {
  const [pendingId, setPendingId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [search, setSearch] = useState('')

  // Same search box the menu and ingredient tables on /admin/menu already have, matching the
  // name or the code -- a customer asking about "JESIEN10" meant scanning every row for it,
  // and the list only grows since promotions are deactivated, never deleted.
  const filteredPromotions = useMemo(() => {
    const query = search.trim().toLowerCase()
    if (!query) {
      return promotions
    }
    return promotions.filter(
      (promotion) =>
        promotion.name.toLowerCase().includes(query) || (promotion.code ?? '').toLowerCase().includes(query),
    )
  }, [promotions, search])

  async function deactivate(promotion: Promotion) {
    // Deactivation is one-way (there's no "activate" action), yet it fired on a single click --
    // same window.confirm guard EmployeeOrderRow uses before cancelling an order.
    if (!window.confirm(`Dezaktywować promocję "${promotion.name}"? Tej operacji nie można cofnąć.`)) {
      return
    }
    setPendingId(promotion.id)
    setError(null)
    try {
      await deactivatePromotion(promotion.id)
      onDeactivated()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Nie udało się dezaktywować promocji.')
    } finally {
      setPendingId(null)
    }
  }

  if (promotions.length === 0) {
    return <p className="empty-state">Brak zdefiniowanych promocji.</p>
  }

  return (
    <div className="admin-table-wrap">
      {error && <p className="checkout-error">{error}</p>}
      <div className="menu-filters">
        <input
          type="search"
          className="menu-filters__search"
          placeholder="Szukaj promocji lub kodu..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          aria-label="Szukaj promocji"
        />
      </div>
      {/* Same no-match message as MenuItemList/IngredientsSection. */}
      {filteredPromotions.length === 0 ? (
        <p className="empty-state">Brak promocji spełniających kryteria wyszukiwania.</p>
      ) : (
        <table className="admin-table">
          <thead>
            <tr>
              <th>Nazwa</th>
              <th>Kod</th>
              <th>Typ</th>
              <th>Wartość</th>
              <th>Okno ważności</th>
              <th>Status</th>
              <th>Limit/Użycia</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {filteredPromotions.map((promotion) => (
              <tr key={promotion.id}>
                <td>{promotion.name}</td>
                <td>{promotion.code ?? '—'}</td>
                <td>{TYPE_LABELS[promotion.type]}</td>
                <td>{formatValue(promotion)}</td>
                <td>
                  {formatDate(promotion.validFrom)} – {formatDate(promotion.validTo)}
                </td>
                <td>{promotionStatus(promotion)}</td>
                <td>
                  {promotion.usageCount}
                  {promotion.usageLimit !== null ? ` / ${promotion.usageLimit}` : ''}
                </td>
                <td>
                  <button type="button" onClick={() => onEdit(promotion)}>
                    Edytuj
                  </button>
                  {promotion.isActive && (
                    <button
                      type="button"
                      disabled={pendingId === promotion.id}
                      onClick={() => void deactivate(promotion)}
                    >
                      Dezaktywuj
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}
