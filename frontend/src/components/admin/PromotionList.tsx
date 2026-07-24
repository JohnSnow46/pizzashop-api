import type { Promotion } from '../../api/types'

interface PromotionListProps {
  promotions: Promotion[]
  onEdit: (promotion: Promotion) => void
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

/** Read-only table of all promotions with an edit action (admin promotions UI). */
export function PromotionList({ promotions, onEdit }: PromotionListProps) {
  if (promotions.length === 0) {
    return <p className="empty-state">Brak zdefiniowanych promocji.</p>
  }

  return (
    <div className="admin-table-wrap">
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
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
