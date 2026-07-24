import { DeliveryAreaForm } from '../components/admin/DeliveryAreaForm'
import { OpeningHoursForm } from '../components/admin/OpeningHoursForm'
import { OrderingThresholdsForm } from '../components/admin/OrderingThresholdsForm'
import { useRestaurantConfig } from '../hooks/useRestaurantConfig'

/**
 * `/admin/restaurant` (RequireAuth roles=RestaurantAdmin/SuperAdmin, mirrors AuthRoles.Admin) —
 * restaurant configuration: opening hours, delivery area, ordering/free-delivery thresholds.
 */
export function AdminRestaurantPage() {
  const { config, loading, error, reload } = useRestaurantConfig()

  return (
    <div className="admin-page">
      <h2>Panel admina — restauracja</h2>

      {loading && <p>Ładowanie konfiguracji...</p>}
      {error && <p className="empty-state">{error}</p>}

      {config && (
        <>
          <section className="admin-section">
            <OpeningHoursForm openingHours={config.openingHours} onSaved={reload} />
          </section>

          <section className="admin-section">
            <DeliveryAreaForm location={config.location} deliveryRadiusKm={config.deliveryRadiusKm} onSaved={reload} />
          </section>

          <section className="admin-section">
            <OrderingThresholdsForm
              minimumOrderValue={config.minimumOrderValue}
              freeDeliveryThreshold={config.freeDeliveryThreshold}
              deliveryFee={config.deliveryFee}
              onSaved={reload}
            />
          </section>
        </>
      )}
    </div>
  )
}
