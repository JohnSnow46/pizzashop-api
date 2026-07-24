import { useState } from 'react'
import { deactivatePromotion } from '../../api/promotionsApi'
import type { Promotion } from '../../api/types'

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

function formatValue(promotion: Promotion): string {
  if (promotion.type === 'Percentage') {
    return promotion.value !== null ? `${promotion.value}%` : '—'
  }
  if (promotion.type === 'FixedAmount') {
    return promotion.value !== null ? `${promotion.value.toFixed(2)} PLN` : '—'
  }
  return '—'
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('pl-PL')
}

/** Read-only table of all promotions with an edit action and a (one-way) deactivate action. */
export function PromotionList({ promotions, onEdit, onDeactivated }: PromotionListProps) {
  const [pendingId, setPendingId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  async function deactivate(promotion: Promotion) {
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
      <table className="admin-table">
        <thead>
          <tr>
            <th>Nazwa</th>
            <th>Kod</th>
            <th>Typ</th>
            <th>Wartość</th>
            <th>Okno ważności</th>
            <th>Aktywna</th>
            <th>Limit/Użycia</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {promotions.map((promotion) => (
            <tr key={promotion.id}>
              <td>{promotion.name}</td>
              <td>{promotion.code ?? '—'}</td>
              <td>{TYPE_LABELS[promotion.type]}</td>
              <td>{formatValue(promotion)}</td>
              <td>
                {formatDate(promotion.validFrom)} – {formatDate(promotion.validTo)}
              </td>
              <td>{promotion.isActive ? 'Tak' : 'Nie'}</td>
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
    </div>
  )
}
